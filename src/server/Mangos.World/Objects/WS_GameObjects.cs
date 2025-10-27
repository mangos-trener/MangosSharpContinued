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

using Mangos.Common.Enums.GameObject;
using Mangos.Common.Enums.Global;
using Mangos.Common.Enums.Spell;
using Mangos.Common.Globals;
using Mangos.Common.Legacy;
using Mangos.Common.Legacy.Databases;
using Mangos.World.Globals;
using Mangos.World.Handlers;
using Mangos.World.Loots;
using Mangos.World.Maps;
using Mangos.World.Network;
using Mangos.World.Objects.Factories.Loot;
using Mangos.World.Objects.Factories.Packets;
using Mangos.World.Player;
using Mangos.World.Services;
using Mangos.World.Spells;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Mangos.World.Objects;

public class WS_GameObjects
{
    private readonly ILogger<WS_GameObjects> logger;
    private readonly WorldState worldState;
    private readonly WorldDatabase worldDatabase;
    private readonly WS_Maps maps;
    private readonly IMapTileLoader mapTileLoader;
    private readonly UpdateClassFactory updateClassFactory;
    private readonly LootObjectFactory lootObjectFactory;

    public WS_GameObjects(
        ILogger<WS_GameObjects> logger,
        WorldState worldState,
        WorldDatabase worldDatabase,
        WS_Maps maps,
        IMapTileLoader mapTileLoader,
        UpdateClassFactory updateClassFactory,
        LootObjectFactory lootObjectFactory)
    {
        this.logger = logger;
        this.worldState = worldState;
        this.worldDatabase = worldDatabase;
        this.maps = maps;
        this.mapTileLoader = mapTileLoader;
        this.updateClassFactory = updateClassFactory;
        this.lootObjectFactory = lootObjectFactory;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static ulong GetNewGUID()
    {
        return ++WorldState.GameObjectsGuidCounter;
    }

    public GameObject GetClosestGameobject(ref WS_Base.BaseUnit unit, int GameObjectEntry = 0)
    {
        var minDistance = float.MaxValue;
        GameObject targetGameobject = null;
        if (unit is CharacterObject @object)
        {
            var array = @object.gameObjectsNear.ToArray();
            foreach (var GUID2 in array)
            {
                if (worldState.WorldGameObjects.ContainsKey(GUID2) && (GameObjectEntry == 0 || worldState.WorldGameObjects[GUID2].ID == GameObjectEntry))
                {
                    var tmpDistance = WS_Combat.GetDistance(worldState.WorldGameObjects[GUID2], unit);
                    if (tmpDistance < minDistance)
                    {
                        minDistance = tmpDistance;
                        targetGameobject = worldState.WorldGameObjects[GUID2];
                    }
                }
            }
            return targetGameobject;
        }
        byte cellX = default;
        byte cellY = default;
        maps.GetMapTile(unit.positionX, unit.positionY, ref cellX, ref cellY);
        var x = -1;
        checked
        {
            do
            {
                var y = -1;
                do
                {
                    if (x + cellX > -1 && x + cellX < 64 && y + cellY > -1 && y + cellY < 64 && maps.Maps[unit.MapID].Tiles[x + cellX, y + cellY] != null)
                    {
                        var gameobjects = maps.Maps[unit.MapID].Tiles[x + cellX, y + cellY].GameObjectsHere.ToArray();
                        var array2 = gameobjects;
                        foreach (var GUID in array2)
                        {
                            if (worldState.WorldGameObjects.ContainsKey(GUID) && (GameObjectEntry == 0 || worldState.WorldGameObjects[GUID].ID == GameObjectEntry))
                            {
                                var tmpDistance = WS_Combat.GetDistance(worldState.WorldGameObjects[GUID], unit);
                                if (tmpDistance < minDistance)
                                {
                                    minDistance = tmpDistance;
                                    targetGameobject = worldState.WorldGameObjects[GUID];
                                }
                            }
                        }
                    }
                    y++;
                }
                while (y <= 1);
                x++;
            }
            while (x <= 1);
            return targetGameobject;
        }
    }

    public void On_CMSG_GAMEOBJECT_QUERY(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        checked
        {
            if (packet.Data.Length - 1 < 17)
            {
                return;
            }
            Packets.PacketClass response = new(Opcodes.SMSG_GAMEOBJECT_QUERY_RESPONSE);
            packet.GetInt16();
            var GameObjectID = packet.GetInt32();
            var GameObjectGUID = packet.GetUInt64();
            try
            {
                if (!worldState.GameObjectsDatabase.ContainsKey(GameObjectID))
                {
                    logger.LogDebug("[{0}:{1}] CMSG_GAMEOBJECT_QUERY [GameObject {2} not loaded.]", client.IP, client.Port, GameObjectID);
                    response.AddUInt32((uint)(GameObjectID | int.MinValue));
                    client.Send(ref response);
                    response.Dispose();
                    return;
                }
                var GameObject = worldState.GameObjectsDatabase[GameObjectID];
                response.AddInt32(GameObject.ID);
                response.AddInt32((int)GameObject.Type);
                response.AddInt32(GameObject.Model);
                response.AddString(GameObject.Name);
                response.AddInt16(0);
                response.AddInt8(0);
                response.AddInt8(0);
                byte i = 0;
                do
                {
                    response.AddUInt32(GameObject.Fields[i]);
                    i = (byte)unchecked((uint)(i + 1));
                }
                while (i <= 23u);
                client.Send(ref response);
                response.Dispose();
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                logger.LogError("Unknown Error: Unable to find GameObjectID={0} in database.", GameObjectID);
                ProjectData.ClearProjectError();
            }
        }
    }

    public void On_CMSG_GAMEOBJ_USE(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        checked
        {
            if (packet.Data.Length - 1 < 13)
            {
                return;
            }
            packet.GetInt16();
            var GameObjectGUID = packet.GetUInt64();
            logger.LogDebug("[{0}:{1}] CMSG_GAMEOBJ_USE [GUID={2:X}]", client.IP, client.Port, GameObjectGUID);
            if (!worldState.WorldGameObjects.ContainsKey(GameObjectGUID))
            {
                return;
            }
            var GO = worldState.WorldGameObjects[GameObjectGUID];
            client.Character.RemoveAurasByInterruptFlag(2048);
            logger.LogDebug("GameObjectType: {0}", worldState.WorldGameObjects[GameObjectGUID].Type);
            var type = GO.Type;
            switch (type)
            {
                case GameObjectType.GAMEOBJECT_TYPE_QUESTGIVER:
                    if (type == GameObjectType.GAMEOBJECT_TYPE_QUESTGIVER)
                    {
                        var qm = worldState.QuestsService.GetQuestMenuGO(ref client.Character, GameObjectGUID);
                        worldState.QuestsService.SendQuestMenu(ref client.Character, GameObjectGUID, "Available quests", qm);
                    }
                    break;

                case GameObjectType.GAMEOBJECT_TYPE_DOOR:
                case GameObjectType.GAMEOBJECT_TYPE_BUTTON:
                    GO.OpenDoor();
                    break;

                case GameObjectType.GAMEOBJECT_TYPE_CHAIR:
                    {
                        Packets.PacketClass StandState = new(Opcodes.CMSG_STANDSTATECHANGE);
                        try
                        {
                            StandState.AddInt8((byte)(4L + worldState.WorldGameObjects[GameObjectGUID].GetSound(1)));
                            client.Character.Teleport(GO.positionX, GO.positionY, GO.positionZ, GO.orientation, (int)GO.MapID);
                            client.Send(ref StandState);
                        }
                        finally
                        {
                            StandState.Dispose();
                        }
                        Packets.PacketClass packetACK = new(Opcodes.SMSG_STANDSTATE_CHANGE_ACK);
                        try
                        {
                            packetACK.AddInt8((byte)(4L + GO.GetSound(1)));
                            client.Send(ref packetACK);
                        }
                        finally
                        {
                            packetACK.Dispose();
                        }
                        break;
                    }
                case GameObjectType.GAMEOBJECT_TYPE_CAMERA:
                    {
                        Packets.PacketClass cinematicPacket = new(Opcodes.SMSG_TRIGGER_CINEMATIC);
                        cinematicPacket.AddUInt32(GO.GetSound(1));
                        client.Send(ref cinematicPacket);
                        cinematicPacket.Dispose();
                        break;
                    }
                case GameObjectType.GAMEOBJECT_TYPE_RITUAL:
                    logger.LogDebug("Clicked a ritual.");
                    if ((GO.Owner == client.Character.GUID || client.Character.IsInGroup) && (GO.Owner == client.Character.GUID || (worldState.Characters.ContainsKey(GO.Owner) && worldState.Characters[GO.Owner].IsInGroup && worldState.Characters[GO.Owner].Group == client.Character.Group)))
                    {
                        logger.LogDebug("Casting ritual spell.");
                        client.Character.CastOnSelf((int)GO.GetSound(1));
                    }
                    break;

                case GameObjectType.GAMEOBJECT_TYPE_SPELLCASTER:
                    logger.LogDebug("Clicked a spellcaster.");
                    GO.Flags = 2;
                    if (GO.GetSound(2) != 0)
                    {
                        logger.LogDebug("Spellcaster requires same group.");
                        logger.LogDebug("Owner: {0:X}  You: {1:X}", worldState.WorldGameObjects[GameObjectGUID].Owner, client.Character.GUID);
                        if ((GO.Owner != client.Character.GUID && !client.Character.IsInGroup) || (GO.Owner != client.Character.GUID && (!worldState.Characters.ContainsKey(GO.Owner) || !worldState.Characters[GO.Owner].IsInGroup || worldState.Characters[GO.Owner].Group != client.Character.Group)))
                        {
                            break;
                        }
                    }
                    logger.LogDebug("Casted spellcaster spell.");
                    client.Character.CastOnSelf((int)GO.GetSound(0));
                    break;

                case GameObjectType.GAMEOBJECT_TYPE_MEETINGSTONE:
                    if (client.Character.Level < GO.GetSound(0))
                    {
                        WS_Spells.SendCastResult(SpellFailedReason.SPELL_FAILED_LEVEL_REQUIREMENT, ref client, 23598);
                    }
                    else if (client.Character.Level > worldState.WorldGameObjects[GameObjectGUID].GetSound(1))
                    {
                        WS_Spells.SendCastResult(SpellFailedReason.SPELL_FAILED_LEVEL_REQUIREMENT, ref client, 23598);
                    }
                    else
                    {
                        client.Character.CastOnSelf(23598);
                    }
                    break;

                case GameObjectType.GAMEOBJECT_TYPE_FISHINGNODE:
                    if (GO.Owner != client.Character.GUID)
                    {
                        break;
                    }
                    if (GO.Loot == null)
                    {
                        if (GO.State == GameObjectLootState.DOOR_CLOSED)
                        {
                            GO.State = GameObjectLootState.DOOR_OPEN;
                            Packets.PacketClass fishNotHookedPacket = new(Opcodes.SMSG_FISH_NOT_HOOKED);
                            client.Send(ref fishNotHookedPacket);
                            fishNotHookedPacket.Dispose();
                        }
                    }
                    else
                    {
                        var AreaFlag = maps.GetAreaFlag(GO.positionX, GO.positionY, (int)GO.MapID);
                        var AreaID = maps.AreaTable[AreaFlag].ID;
                        DataTable MySQLQuery = new();
                        worldDatabase.Query($"SELECT * FROM skill_fishing_base_level WHERE entry = {AreaID};", ref MySQLQuery);
                        if (MySQLQuery.Rows.Count == 0)
                        {
                            AreaID = maps.AreaTable[AreaFlag].Zone;
                            MySQLQuery.Clear();
                            worldDatabase.Query($"SELECT * FROM skill_fishing_base_level WHERE entry = {AreaID};", ref MySQLQuery);
                        }
                        var zoneSkill = 0;
                        if (MySQLQuery.Rows.Count > 0)
                        {
                            zoneSkill = MySQLQuery.Rows[0].As<int>("skill");
                        }
                        else
                        {
                            logger.LogCritical("No fishing entry in 'skill_fishing_base_level' for area [{0}] in zone [{1}]", maps.AreaTable[AreaFlag].ID, maps.AreaTable[AreaFlag].Zone);
                        }
                        int skill = client.Character.Skills[356].CurrentWithBonus;
                        var chance = skill - zoneSkill + 5;
                        var roll = WorldState.Rnd.Next(1, 101);
                        if (skill > zoneSkill && roll >= chance)
                        {
                            GO.State = GameObjectLootState.DOOR_CLOSED;
                            GO.Loot.SendLoot(ref client);
                            client.Character.UpdateSkill(356, 0.01f);
                        }
                        else
                        {
                            GO.State = GameObjectLootState.DOOR_CLOSED;
                            Packets.PacketClass fishEscaped = new(Opcodes.SMSG_FISH_ESCAPED);
                            client.Send(ref fishEscaped);
                            fishEscaped.Dispose();
                        }
                    }
                    client.Character.FinishSpell(CurrentSpellTypes.CURRENT_CHANNELED_SPELL, OK: true);
                    break;
            }
        }
    }

    public void AddToWorld(GameObject gameObject)
    {
        maps.GetMapTile(gameObject.positionX, gameObject.positionY, ref gameObject.CellX, ref gameObject.CellY);
        if (maps.Maps[gameObject.MapID].Tiles[gameObject.CellX, gameObject.CellY] == null)
        {
            mapTileLoader.LoadMap(gameObject.CellX, gameObject.CellY, gameObject.MapID);
        }
        try
        {
            maps.Maps[gameObject.MapID].Tiles[gameObject.CellX, gameObject.CellY].GameObjectsHere.Add(gameObject.GUID);
        }
        catch (Exception projectError)
        {
            ProjectData.SetProjectError(projectError);
            ProjectData.ClearProjectError();
            return;
        }
        if (gameObject.Type == GameObjectType.GAMEOBJECT_TYPE_CHEST && gameObject.Loot == null)
        {
            GenerateLoot(gameObject);
        }
        short i = -1;
        checked
        {
            do
            {
                short j = -1;
                do
                {
                    if ((short)unchecked(gameObject.CellX + i) >= 0 && (short)unchecked(gameObject.CellX + i) <= 63 && (short)unchecked(gameObject.CellY + j) >= 0 && (short)unchecked(gameObject.CellY + j) <= 63 && maps.Maps[gameObject.MapID].Tiles[(short)unchecked(gameObject.CellX + i), (short)unchecked(gameObject.CellY + j)] != null && maps.Maps[gameObject.MapID].Tiles[(short)unchecked(gameObject.CellX + i), (short)unchecked(gameObject.CellY + j)].PlayersHere.Count > 0)
                    {
                        var tMapTile = maps.Maps[gameObject.MapID].Tiles[(short)unchecked(gameObject.CellX + i), (short)unchecked(gameObject.CellY + j)];
                        var list = tMapTile.PlayersHere.ToArray();
                        var array = list;
                        foreach (var plGUID in array)
                        {
                            int num;
                            if (worldState.Characters.ContainsKey(plGUID))
                            {
                                var characterObject = worldState.Characters[plGUID];
                                WS_Base.BaseObject objCharacter = gameObject;
                                num = characterObject.CanSee(ref objCharacter) ? 1 : 0;
                            }
                            else
                            {
                                num = 0;
                            }
                            if (num != 0)
                            {
                                Packets.PacketClass packet = new(Opcodes.SMSG_UPDATE_OBJECT);
                                packet.AddInt32(1);
                                packet.AddInt8(0);
                                var tmpUpdate = updateClassFactory.Create(MangosGlobalConstants.FIELD_MASK_SIZE_GAMEOBJECT);
                                Dictionary<ulong, CharacterObject> cHARACTERs;
                                ulong key;
                                var Character = (cHARACTERs = worldState.Characters)[key = plGUID];
                                gameObject.FillAllUpdateFlags(ref tmpUpdate, ref Character);
                                cHARACTERs[key] = Character;
                                var updateClass = tmpUpdate;
                                var updateObject = gameObject;
                                updateClass.AddToPacket(ref packet, ObjectUpdateType.UPDATETYPE_CREATE_OBJECT, ref updateObject);
                                tmpUpdate.Dispose();
                                worldState.Characters[plGUID].client.SendMultiplyPackets(ref packet);
                                worldState.Characters[plGUID].gameObjectsNear.Add(gameObject.GUID);
                                gameObject.SeenBy.Add(plGUID);
                                packet.Dispose();
                            }
                        }
                    }
                    j = (short)unchecked(j + 1);
                }
                while (j <= 1);
                i = (short)unchecked(i + 1);
            }
            while (i <= 1);
        }
    }

    public bool GenerateLoot(GameObject gameObject)
    {
        if (gameObject.Loot != null)
        {
            return true;
        }
        if (gameObject.LootID == 0)
        {
            return false;
        }
        gameObject.Loot = lootObjectFactory.Create(gameObject.GUID, LootType.LOOTTYPE_SKINNING);
        WS_Loot.LootTemplates_Gameobject.GetLoot(gameObject.LootID)?.Process(ref gameObject.Loot, 0);
        gameObject.Loot.LootOwner = 0uL;
        return true;
    }

    public void Respawn(GameObject gameObject)
    {
        logger.LogDebug("Gameobject {0:X} respawning.", gameObject.GUID);
        if (gameObject.RespawnTimer != null)
        {
            gameObject.RespawnTimer.Dispose();
            gameObject.RespawnTimer = null;
            gameObject.Despawned = false;
        }

        gameObject.Loot = null;
        AddToWorld(gameObject);
        gameObject.CalculateMineRemaning(Force: true);
    }

    public void Despawn(GameObject gameObject, int Delay = 0)
    {
        if (Delay == 0)
        {
            logger.LogDebug("Gameobject {0:X} despawning.", gameObject.GUID);
            Packets.PacketClass packet = new(Opcodes.SMSG_GAMEOBJECT_DESPAWN_ANIM);
            packet.AddUInt64(gameObject.GUID);
            gameObject.SendToNearPlayers(ref packet);
            packet.Dispose();
            gameObject.Despawned = true;
            gameObject.Loot?.Dispose();
            gameObject.RemoveFromWorld();

            if (gameObject.SpawnTime > 0)
            {
                gameObject.RespawnTimer = new Timer(x => Respawn(gameObject), null, gameObject.SpawnTime, -1);
            }
        }
        else
        {
            gameObject.ToDespawn = true;
            gameObject.RespawnTimer = new Timer(x => Destroy(gameObject), null, Delay, -1);
        }
    }

    public void Destroy(GameObject gameObject)
    {
        if (gameObject.RespawnTimer != null)
        {
            gameObject.RespawnTimer.Dispose();
            gameObject.RespawnTimer = null;
            gameObject.Despawned = false;
        }

        if (gameObject.CreatedBySpell > 0 && decimal.Compare(new decimal(gameObject.Owner), 0m) > 0
            && worldState.Characters.ContainsKey(gameObject.Owner)
            && worldState.Characters[gameObject.Owner].gameObjects.Contains(gameObject))
        {
            worldState.Characters[gameObject.Owner].gameObjects.Remove(gameObject);
        }

        if (gameObject.ToDespawn)
        {
            gameObject.ToDespawn = false;
            logger.LogDebug("Gameobject {0:X} despawning.", gameObject.GUID);
            Packets.PacketClass despawnPacket = new(Opcodes.SMSG_GAMEOBJECT_DESPAWN_ANIM);
            despawnPacket.AddUInt64(gameObject.GUID);
            gameObject.SendToNearPlayers(ref despawnPacket);
            despawnPacket.Dispose();
        }

        Packets.PacketClass packet = new(Opcodes.SMSG_DESTROY_OBJECT);
        packet.AddUInt64(gameObject.GUID);
        gameObject.SendToNearPlayers(ref packet);

        packet.Dispose();
        gameObject.Dispose();
    }
}
