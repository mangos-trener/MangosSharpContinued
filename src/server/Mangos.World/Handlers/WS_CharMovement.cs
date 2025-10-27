//
// Copyright (C) 2013-2025 getMaNGOS <https://www.getmangos.eu>
//
// This program is free software. You can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation. either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY. Without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//

using Mangos.Common.Enums.Faction;
using Mangos.Common.Enums.Global;
using Mangos.Common.Enums.Player;
using Mangos.Common.Enums.Spell;
using Mangos.Common.Globals;
using Mangos.Common.Legacy;
using Mangos.Common.Legacy.Databases;
using Mangos.Common.Legacy.Globals;
using Mangos.World.AI;
using Mangos.World.AntiCheat;
using Mangos.World.Globals;
using Mangos.World.Maps;
using Mangos.World.Network;
using Mangos.World.Objects;
using Mangos.World.Objects.Factories.Packets;
using Mangos.World.Player;
using Mangos.World.Scripts;
using Mangos.World.Services;
using Mangos.World.Spells;
using Mangos.World.Weather;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Data;

namespace Mangos.World.Handlers;

public class WS_CharMovement
{
    public WS_CharMovement(
        ILogger<WS_CharMovement> logger,
        ICluster cluster,
        IScriptExecutor scriptExecutor,
        WorldState worldState,
        WorldDatabase worldDatabase,
        WS_Maps maps,
        WS_Network network,
        ICharacterResurrectionService characterResurrectionService,
        ICellUpdater cellUpdater,
        UpdateClassFactory updateClassFactory)
    {
        this.logger = logger;
        this.cluster = cluster;
        this.scriptExecutor = scriptExecutor;
        this.worldState = worldState;
        this.worldDatabase = worldDatabase;
        this.maps = maps;
        this.network = network;
        this.characterResurrectionService = characterResurrectionService;
        this.cellUpdater = cellUpdater;
        this.updateClassFactory = updateClassFactory;
    }

    private const float PId2 = (float)Math.PI / 2f;

    private const float PIx2 = (float)Math.PI * 2f;
    private readonly ILogger<WS_CharMovement> logger;
    private readonly ICluster cluster;
    private readonly IScriptExecutor scriptExecutor;
    private readonly WorldState worldState;
    private readonly WorldDatabase worldDatabase;
    private readonly WS_Maps maps;
    private readonly WS_Network network;
    private readonly ICharacterResurrectionService characterResurrectionService;
    private readonly ICellUpdater cellUpdater;
    private readonly UpdateClassFactory updateClassFactory;

    public void OnMovementPacket(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        packet.GetInt16();
        if (client.Character != null && client.Character.MindControl != null)
        {
            OnControlledMovementPacket(ref packet, ref client.Character.MindControl, ref client.Character);
            return;
        }
        if (client.Character != null)
        {
            client.Character.charMovementFlags = packet.GetInt32();
        }
        var Time = packet.GetUInt32();
        var posX = packet.GetFloat();
        var posY = packet.GetFloat();
        var posZ = packet.GetFloat();
        if (client.Character != null)
        {
            client.Character.orientation = packet.GetFloat();
            WS_Anticheat.MovementEvent(logger, ref client, client.Character.RunSpeed, posX, client.Character.positionX, posY, client.Character.positionY, posZ, client.Character.positionZ, checked((int)Time), network.MsTime());
        }
        if (client.Character == null)
        {
            return;
        }
        client.Character.positionX = posX;
        client.Character.positionY = posY;
        client.Character.positionZ = posZ;
        if (client.Character.positionZ < -500f)
        {
            worldState.GraveyardsService.GoToNearestGraveyard(ref client.Character, Alive: false, Teleport: true);
            return;
        }
        if (client.Character.Pet != null && client.Character.Pet.FollowOwner)
        {
            var angle = client.Character.orientation - ((float)Math.PI / 2f);
            if (angle < 0f)
            {
                angle += (float)Math.PI * 2f;
            }
            client.Character.Pet.SetToRealPosition();
            var tmpX = (float)(client.Character.positionX + (Math.Cos(angle) * 2.0));
            var tmpY = (float)(client.Character.positionY + (Math.Sin(angle) * 2.0));
            client.Character.Pet.MoveTo(tmpX, tmpY, client.Character.positionZ, client.Character.orientation, Running: true);
        }
        if (((uint)client.Character.charMovementFlags & 0x2000000u) != 0)
        {
            var transportGUID = packet.GetUInt64();
            var transportX = packet.GetFloat();
            var transportY = packet.GetFloat();
            var transportZ = packet.GetFloat();
            var transportO = packet.GetFloat();
            client.Character.transportX = transportX;
            client.Character.transportY = transportY;
            client.Character.transportZ = transportZ;
            client.Character.transportO = transportO;
            if (client.Character.OnTransport == null)
            {
                if (LegacyGlobalFunctions.GuidIsMoTransport(transportGUID) && worldState.WorldTransports.ContainsKey(transportGUID))
                {
                    client.Character.OnTransport = worldState.WorldTransports[transportGUID];
                    var character = client.Character;
                    var NotSpellID = 0;
                    character.RemoveAurasOfType(AuraEffects_Names.SPELL_AURA_MOUNTED, NotSpellID);
                    WS_Transports.TransportObject obj = (WS_Transports.TransportObject)client.Character.OnTransport;
                    ref var character2 = ref client.Character;
                    ref var reference = ref character2;
                    WS_Base.BaseUnit Unit = character2;
                    obj.AddPassenger(ref Unit);
                    reference = (CharacterObject)Unit;
                }
                else if (LegacyGlobalFunctions.GuidIsTransport(transportGUID) && worldState.WorldGameObjects.ContainsKey(transportGUID))
                {
                    client.Character.OnTransport = worldState.WorldGameObjects[transportGUID];
                }
            }
        }
        else if (client.Character.OnTransport != null)
        {
            if (client.Character.OnTransport is WS_Transports.TransportObject obj2)
            {
                ref var character3 = ref client.Character;
                ref var reference = ref character3;
                WS_Base.BaseUnit Unit = character3;
                obj2.RemovePassenger(ref Unit);
                reference = (CharacterObject)Unit;
            }
            client.Character.OnTransport = null;
        }
        if (((uint)client.Character.charMovementFlags & 0x200000u) != 0)
        {
            var swimAngle = packet.GetFloat();
        }
        packet.GetInt32();
        if (((uint)client.Character.charMovementFlags & 0x2000u) != 0)
        {
            var airTime = packet.GetUInt32();
            var sinAngle = packet.GetFloat();
            var cosAngle = packet.GetFloat();
            var xySpeed = packet.GetFloat();
        }
        if (((uint)client.Character.charMovementFlags & 0x4000000u) != 0)
        {
            var unk1 = packet.GetFloat();
        }
        checked
        {
            if (client.Character.exploreCheckQueued_ && !client.Character.DEAD)
            {
                var exploreFlag = maps.GetAreaFlag(client.Character.positionX, client.Character.positionY, (int)client.Character.MapID);
                if (exploreFlag != 65535)
                {
                    var areaFlag = exploreFlag % 32;
                    var areaFlagOffset = (byte)(exploreFlag / 32);
                    if (!Functions.HaveFlag(client.Character.ZonesExplored[areaFlagOffset], (byte)areaFlag))
                    {
                        Functions.SetFlag(ref client.Character.ZonesExplored[areaFlagOffset], (byte)areaFlag, flagValue: true);
                        var GainedXP = maps.AreaTable[exploreFlag].Level * 10;
                        GainedXP = maps.AreaTable[exploreFlag].Level * 10;
                        Packets.PacketClass SMSG_EXPLORATION_EXPERIENCE = new(Opcodes.SMSG_EXPLORATION_EXPERIENCE);
                        SMSG_EXPLORATION_EXPERIENCE.AddInt32(maps.AreaTable[exploreFlag].ID);
                        SMSG_EXPLORATION_EXPERIENCE.AddInt32(GainedXP);
                        client.Send(ref SMSG_EXPLORATION_EXPERIENCE);
                        SMSG_EXPLORATION_EXPERIENCE.Dispose();
                        client.Character.SetUpdateFlag(1111 + areaFlagOffset, client.Character.ZonesExplored[areaFlagOffset]);
                        client.Character.AddXP(GainedXP, 0);
                        worldState.QuestsService.OnQuestExplore(ref client.Character, exploreFlag);
                    }
                }
            }
            if (client.Character.IsMoving)
            {
                if (client.Character.cEmoteState > 0)
                {
                    client.Character.cEmoteState = 0;
                    client.Character.SetUpdateFlag(148, client.Character.cEmoteState);
                    client.Character.SendCharacterUpdate();
                }
                if (client.Character.spellCasted[1] != null)
                {
                    var castSpellParameters = client.Character.spellCasted[1];
                    if (unchecked((0u - ((!castSpellParameters.Finished) ? 1u : 0u)) & (uint)WS_Spells.SPELLs[castSpellParameters.SpellID].interruptFlags & (true ? 1u : 0u)) != 0)
                    {
                        client.Character.FinishSpell(CurrentSpellTypes.CURRENT_GENERIC_SPELL);
                    }
                }
                client.Character.RemoveAurasByInterruptFlag(8);
            }
            if (client.Character.IsTurning)
            {
                client.Character.RemoveAurasByInterruptFlag(16);
            }
            var MsTime = network.MsTime();
            var ClientTimeDelay = (int)(MsTime - Time);
            var MoveTime = (int)(Time - checked(MsTime - ClientTimeDelay) + 500 + MsTime);
            packet.AddInt32(MoveTime, 10);
            Packets.PacketClass response = new(packet.OpCode);
            response.AddPackGUID(client.Character.GUID);
            var tempArray = new byte[packet.Data.Length - 6 + 1];
            Array.Copy(packet.Data, 6, tempArray, 0, packet.Data.Length - 6);
            response.AddByteArray(tempArray);
            client.Character.SendToNearPlayers(ref response, 0uL, ToSelf: false);
            response.Dispose();
            if (client.Character.IsMoving)
            {
                client.Character.RemoveAurasByInterruptFlag(8);
            }
            if (client.Character.IsTurning)
            {
                client.Character.RemoveAurasByInterruptFlag(16);
            }
        }
    }

    public void OnControlledMovementPacket(ref Packets.PacketClass packet, ref WS_Base.BaseUnit Controlled, ref CharacterObject Controller)
    {
        var MovementFlags = packet.GetInt32();
        var Time = packet.GetUInt32();
        var PositionX = packet.GetFloat();
        var PositionY = packet.GetFloat();
        var PositionZ = packet.GetFloat();
        var Orientation = packet.GetFloat();
        if (Controlled is CharacterObject characterObject)
        {
            characterObject.charMovementFlags = MovementFlags;
            characterObject.positionX = PositionX;
            characterObject.positionY = PositionY;
            characterObject.positionZ = PositionZ;
            characterObject.orientation = Orientation;
        }
        else if (Controlled is WS_Creatures.CreatureObject creatureObject)
        {
            creatureObject.positionX = PositionX;
            creatureObject.positionY = PositionY;
            creatureObject.positionZ = PositionZ;
            creatureObject.orientation = Orientation;
        }
        var MsTime = network.MsTime();
        checked
        {
            var ClientTimeDelay = (int)(MsTime - Time);
            var MoveTime = (int)(Time - checked(MsTime - ClientTimeDelay) + 500 + MsTime);
            packet.AddInt32(MoveTime, 10);
            Packets.PacketClass response = new(packet.OpCode);
            response.AddPackGUID(Controlled.GUID);
            var tempArray = new byte[packet.Data.Length - 6 + 1];
            Array.Copy(packet.Data, 6, tempArray, 0, packet.Data.Length - 6);
            response.AddByteArray(tempArray);
            Controlled.SendToNearPlayers(ref response, Controller.GUID);
            response.Dispose();
        }
    }

    public void OnStartSwim(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        OnMovementPacket(ref packet, ref client);
        if (client.Character.positionZ < maps.GetWaterLevel(client.Character.positionX, client.Character.positionY, checked((int)client.Character.MapID)))
        {
            if (client.Character.underWaterTimer == null && !client.Character.underWaterBreathing && !client.Character.DEAD)
            {
                client.Character.underWaterTimer = new WS_PlayerHelper.TDrowningTimer(worldState, ref client.Character);
            }
        }
        else if (client.Character.underWaterTimer != null)
        {
            client.Character.underWaterTimer.Dispose();
            client.Character.underWaterTimer = null;
        }
    }

    public void OnStopSwim(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        if (client.Character.underWaterTimer != null)
        {
            client.Character.underWaterTimer.Dispose();
            client.Character.underWaterTimer = null;
        }
        OnMovementPacket(ref packet, ref client);
    }

    public void OnChangeSpeed(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        packet.GetInt16();
        var GUID = packet.GetUInt64();
        if (GUID == client.Character.GUID)
        {
            packet.GetInt32();
            var flags = packet.GetInt32();
            var time = packet.GetInt32();
            client.Character.positionX = packet.GetFloat();
            client.Character.positionY = packet.GetFloat();
            client.Character.positionZ = packet.GetFloat();
            client.Character.orientation = packet.GetFloat();
            if (((uint)flags & 0x2000000u) != 0)
            {
                packet.GetInt64();
                packet.GetFloat();
                packet.GetFloat();
                packet.GetFloat();
                packet.GetFloat();
            }
            if (((uint)flags & 0x200000u) != 0)
            {
                packet.GetFloat();
            }
            float falltime = packet.GetInt32();
            if (((uint)flags & 0x2000u) != 0)
            {
                packet.GetFloat();
                packet.GetFloat();
                packet.GetFloat();
                packet.GetFloat();
            }
            var newSpeed = packet.GetFloat();
            checked
            {
                client.Character.antiHackSpeedChanged_--;
                switch (packet.OpCode)
                {
                    case Opcodes.CMSG_FORCE_RUN_SPEED_CHANGE_ACK:
                        client.Character.RunSpeed = newSpeed;
                        break;

                    case Opcodes.CMSG_FORCE_RUN_BACK_SPEED_CHANGE_ACK:
                        client.Character.RunBackSpeed = newSpeed;
                        break;

                    case Opcodes.CMSG_FORCE_SWIM_BACK_SPEED_CHANGE_ACK:
                        client.Character.SwimBackSpeed = newSpeed;
                        break;

                    case Opcodes.CMSG_FORCE_SWIM_SPEED_CHANGE_ACK:
                        client.Character.SwimSpeed = newSpeed;
                        break;

                    case Opcodes.CMSG_FORCE_TURN_RATE_CHANGE_ACK:
                        client.Character.TurnRate = newSpeed;
                        break;
                }
            }
        }
    }

    public void SendAreaTriggerMessage(ref WS_Network.ClientClass client, string Text)
    {
        Packets.PacketClass p = new(Opcodes.SMSG_AREA_TRIGGER_MESSAGE);
        p.AddInt32(Text.Length);
        p.AddString(Text);
        client.Send(ref p);
        p.Dispose();
    }

    public void On_CMSG_AREATRIGGER(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        try
        {
            if (checked(packet.Data.Length - 1) < 9)
            {
                return;
            }
            packet.GetInt16();
            var triggerID = packet.GetInt32();
            logger.LogDebug("[{0}:{1}] CMSG_AREATRIGGER [triggerID={2}]", client.IP, client.Port, triggerID);
            DataTable q = new();
            q.Clear();
            worldDatabase.Query($"SELECT entry, quest FROM quest_relations WHERE actor=2 and role=0 and entry = {triggerID};", ref q);
            if (q.Rows.Count > 0)
            {
                worldState.QuestsService.OnQuestExplore(ref client.Character, triggerID);
                return;
            }
            q.Clear();
            worldDatabase.Query($"SELECT * FROM areatrigger_tavern WHERE id = {triggerID};", ref q);
            if (q.Rows.Count > 0)
            {
                client.Character.cPlayerFlags |= PlayerFlags.PLAYER_FLAGS_RESTING;
                client.Character.SetUpdateFlag(190, (int)client.Character.cPlayerFlags);
                client.Character.SendCharacterUpdate();
                return;
            }
            q.Clear();
            worldDatabase.Query($"SELECT * FROM areatrigger_teleport WHERE id = {triggerID};", ref q);
            float posX;
            float posY;
            float posZ;
            float ori;
            int tMap;
            byte reqLevel;
            if (q.Rows.Count > 0)
            {
                posX = q.Rows[0].As<float>("target_position_x");
                posY = q.Rows[0].As<float>("target_position_y");
                posZ = q.Rows[0].As<float>("target_position_z");
                ori = q.Rows[0].As<float>("target_orientation");
                tMap = q.Rows[0].As<int>("target_map");
                reqLevel = q.Rows[0].As<byte>("required_level");
                if (!client.Character.DEAD)
                {
                    goto IL_029d;
                }
                if (client.Character.corpseMapID == tMap)
                {
                    characterResurrectionService.CharacterResurrect(ref client.Character);
                    goto IL_029d;
                }
                worldState.GraveyardsService.GoToNearestGraveyard(ref client.Character, Alive: false, Teleport: true);
            }
            else if (!Information.IsNothing(scriptExecutor))
            {
                if (scriptExecutor.ContainsMethod("AreaTriggers", $"HandleAreaTrigger_{triggerID}"))
                {
                    scriptExecutor.InvokeFunction("AreaTriggers", $"HandleAreaTrigger_{triggerID}", new object[1]
                    {
                        client.Character.GUID
                    });
                    return;
                }
                logger.LogWarning("[{0}:{1}] AreaTrigger [{2}] not found!", client.IP, client.Port, triggerID);
            }
            goto end_IL_0001;
        IL_029d:
            if (reqLevel != 0 && client.Character.Level < (uint)reqLevel)
            {
                SendAreaTriggerMessage(ref client, "Your level is too low");
            }
            else if (posX != 0f && posY != 0f && posZ != 0f)
            {
                client.Character.Teleport(posX, posY, posZ, ori, tMap);
            }
        end_IL_0001:
            ;
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);
            var e = ex;
            logger.LogCritical("Error when entering areatrigger.{0}", Environment.NewLine + e);
            ProjectData.ClearProjectError();
        }
    }

    public void On_CMSG_MOVE_TIME_SKIPPED(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
    }

    public void On_MSG_MOVE_FALL_LAND(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        try
        {
            OnMovementPacket(ref packet, ref client);
            packet.Offset = 6;
            var movFlags = packet.GetInt32();
            packet.GetUInt32();
            packet.GetFloat();
            packet.GetFloat();
            packet.GetFloat();
            packet.GetFloat();
            if (((uint)movFlags & 0x2000000u) != 0)
            {
                packet.GetUInt64();
                packet.GetFloat();
                packet.GetFloat();
                packet.GetFloat();
                packet.GetFloat();
            }
            if (((uint)movFlags & 0x200000u) != 0)
            {
                packet.GetFloat();
            }
            var FallTime = packet.GetInt32();
            checked
            {
                if (FallTime > 1100 && !client.Character.DEAD && client.Character.positionZ > maps.GetWaterLevel(client.Character.positionX, client.Character.positionY, (int)client.Character.MapID) && !client.Character.HaveAuraType(AuraEffects_Names.SPELL_AURA_FEATHER_FALL))
                {
                    var safe_fall = client.Character.GetAuraModifier(AuraEffects_Names.SPELL_AURA_SAFE_FALL);
                    if (safe_fall > 0)
                    {
                        FallTime = (FallTime > safe_fall * 10) ? (FallTime - (safe_fall * 10)) : 0;
                    }
                    if (FallTime > 1100)
                    {
                        var FallPerc = (float)(FallTime / 1100.0);
                        var FallDamage = (int)Math.Round(((FallPerc * FallPerc) - 1f) / 9f * client.Character.Life.Maximum);
                        if (FallDamage > 0)
                        {
                            if (FallDamage > client.Character.Life.Maximum)
                            {
                                FallDamage = client.Character.Life.Maximum;
                            }
                            client.Character.LogEnvironmentalDamage(DamageTypes.DMG_FIRE, FallDamage);
                            var character = client.Character;
                            var damage = FallDamage;
                            WS_Base.BaseUnit Attacker = null;
                            character.DealDamage(damage, Attacker);
                            logger.LogInformation("[{0}:{1}] Client fall time: {2}  Damage: {3}", client.IP, client.Port, FallTime, FallDamage);
                        }
                    }
                    if (client.Character.underWaterTimer != null && client.Character != null)
                    {
                        client.Character.underWaterTimer.Dispose();
                        client.Character.underWaterTimer = null;
                    }
                    if (client.Character.LogoutTimer != null)
                    {
                        var UpdateData = updateClassFactory.Create(MangosGlobalConstants.FIELD_MASK_SIZE_PLAYER);
                        Packets.PacketClass SMSG_UPDATE_OBJECT = new(Opcodes.SMSG_UPDATE_OBJECT);
                        try
                        {
                            SMSG_UPDATE_OBJECT.AddInt32(1);
                            SMSG_UPDATE_OBJECT.AddInt8(0);
                            client.Character.cUnitFlags |= 0x40000;
                            UpdateData.SetUpdateFlag(46, client.Character.cUnitFlags);
                            client.Character.StandState = 1;
                            UpdateData.SetUpdateFlag(138, client.Character.cBytes1);
                            UpdateData.AddToPacket(ref SMSG_UPDATE_OBJECT, ObjectUpdateType.UPDATETYPE_VALUES, ref client.Character);
                            client.Send(ref SMSG_UPDATE_OBJECT);
                        }
                        finally
                        {
                            SMSG_UPDATE_OBJECT.Dispose();
                        }
                        Packets.PacketClass packetACK = new(Opcodes.SMSG_STANDSTATE_CHANGE_ACK);
                        try
                        {
                            packetACK.AddInt8(1);
                            client.Send(ref packetACK);
                        }
                        finally
                        {
                            packetACK.Dispose();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);
            var e = ex;
            logger.LogDebug("Error when falling.{0}", Environment.NewLine + e);
            ProjectData.ClearProjectError();
        }
    }

    public void On_CMSG_ZONEUPDATE(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        checked
        {
            if (packet.Data.Length - 1 >= 9 && client.Character != null)
            {
                packet.GetInt16();
                var newZone = packet.GetInt32();
                logger.LogDebug("[{0}:{1}] CMSG_ZONEUPDATE [newZone={2}]", client.IP, client.Port, newZone);
                client.Character.ZoneID = newZone;
                client.Character.exploreCheckQueued_ = true;
                client.Character.ZoneCheck();
                cluster.ClientUpdate(client.Index, (uint)client.Character.ZoneID, client.Character.Level);
                if (WS_Weather.WeatherZones.ContainsKey(newZone))
                {
                    WS_Weather.SendWeather(newZone, ref client);
                }
            }
        }
    }

    public void On_MSG_MOVE_HEARTBEAT(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        OnMovementPacket(ref packet, ref client);
        if (client.Character == null)
        {
            return;
        }
        if ((client.Character.CellX != maps.GetMapTileX(client.Character.positionX)) || (client.Character.CellY != maps.GetMapTileY(client.Character.positionY)))
        {
            cellUpdater.MoveCell(ref client.Character);
        }
        cellUpdater.UpdateCell(ref client.Character);
        client.Character.GroupUpdateFlag |= 0x100u;
        client.Character.ZoneCheck();
        var wS_Maps = maps;
        WS_Base.BaseObject objCharacter = client.Character;
        if (wS_Maps.IsOutsideOfMap(ref objCharacter))
        {
            if (!client.Character.outsideMapID_)
            {
                client.Character.outsideMapID_ = true;
                client.Character.StartMirrorTimer(MirrorTimer.FATIGUE, 30000);
            }
        }
        else if (client.Character.outsideMapID_)
        {
            client.Character.outsideMapID_ = false;
            client.Character.StopMirrorTimer(MirrorTimer.FATIGUE);
        }
        if (client.Character.IsInDuel)
        {
            WS_Spells.CheckDuelDistance(worldState, ref client.Character);
        }
        var array = client.Character.creaturesNear.ToArray();
        foreach (var cGUID in array)
        {
            if (worldState.WorldCreatures.ContainsKey(cGUID)
                && worldState.WorldCreatures[cGUID].aiScript != null
                && (worldState.WorldCreatures[cGUID].aiScript is WS_Creatures_AI.DefaultAI || worldState.WorldCreatures[cGUID].aiScript is WS_Creatures_AI.GuardAI)
                && !worldState.WorldCreatures[cGUID].IsDead
                && !worldState.WorldCreatures[cGUID].aiScript.InCombat
                && !client.Character.inCombatWith.Contains(cGUID)
                && client.Character.GetReaction(worldState.WorldCreatures[cGUID].Faction) == TReaction.HOSTILE
                && WS_Combat.GetDistance(worldState.WorldCreatures[cGUID], client.Character) <= worldState.WorldCreatures[cGUID].AggroRange(client.Character))
            {
                var aiScript = worldState.WorldCreatures[cGUID].aiScript;
                ref var character = ref client.Character;
                WS_Base.BaseUnit Attacker = character;
                aiScript.OnGenerateHate(ref Attacker, 1);
                character = (CharacterObject)Attacker;
                client.Character.AddToCombat(worldState.WorldCreatures[cGUID]);
                worldState.WorldCreatures[cGUID].aiScript.State = AIState.AI_ATTACKING;
                worldState.WorldCreatures[cGUID].aiScript.DoThink();
            }
        }
        var array2 = client.Character.inCombatWith.ToArray();
        foreach (var CombatUnit in array2)
        {
            if (LegacyGlobalFunctions.GuidIsCreature(CombatUnit) && worldState.WorldCreatures.ContainsKey(CombatUnit) && worldState.WorldCreatures[CombatUnit].aiScript != null)
            {
                var creatureObject = worldState.WorldCreatures[CombatUnit];
                if (creatureObject.aiScript.aiTarget != null && creatureObject.aiScript.aiTarget == client.Character)
                {
                    creatureObject.SetToRealPosition();
                    creatureObject.aiScript.State = AIState.AI_MOVE_FOR_ATTACK;
                    creatureObject.aiScript.DoMove();
                }
            }
        }
    }
}
