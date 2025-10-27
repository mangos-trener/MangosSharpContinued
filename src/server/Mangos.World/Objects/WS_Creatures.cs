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

using Mangos.Common.Enums.Chat;
using Mangos.Common.Enums.Faction;
using Mangos.Common.Enums.Global;
using Mangos.Common.Enums.Group;
using Mangos.Common.Enums.Misc;
using Mangos.Common.Enums.Player;
using Mangos.Common.Globals;
using Mangos.Common.Legacy;
using Mangos.Common.Legacy.Databases;
using Mangos.Common.Legacy.Globals;
using Mangos.Configuration;
using Mangos.World.AI;
using Mangos.World.DataStores;
using Mangos.World.Globals;
using Mangos.World.Handlers;
using Mangos.World.Loots;
using Mangos.World.Maps;
using Mangos.World.Network;
using Mangos.World.Objects.Factories;
using Mangos.World.Objects.Factories.CreaturesAi;
using Mangos.World.Objects.Factories.Loot;
using Mangos.World.Objects.Factories.Packets;
using Mangos.World.Objects.Factories.Spells;
using Mangos.World.Player;
using Mangos.World.Scripts;
using Mangos.World.Services;
using Mangos.World.Spells;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Mangos.World.Objects;

public class WS_Creatures
{
    public class CreatureObject : WS_Base.BaseUnit, IDisposable
    {
        private readonly IScriptExecutor scriptExecutor;
        private protected readonly MangosConfiguration configuration;
        private protected readonly WS_DBCDatabase database;
        private protected readonly WorldDatabase worldDatabase;
        private protected readonly WS_Network network;
        private readonly IMapTileLoader tileLoader;
        private readonly LootObjectFactory lootObjectFactory;
        private readonly CreatureObjectFactory creatureObjectFactory;
        private protected readonly WS_Maps maps;
        private protected readonly WS_Creatures creatures;
        private protected readonly WS_Combat combat;
        private protected readonly WS_Loot loot;
        private protected readonly WS_Handlers_Taxi taxi;
        private protected readonly CreatureInfoFactory creatureInfoFactory;
        private readonly SpellTargetsFactory spellTargetsFactory;
        private readonly CastSpellParametersFactory castSpellParametersFactory;
        private readonly UpdateClassFactory updateClassFactory;
        private readonly CritterAiFactory critterAiFactory;
        private readonly DefaultAiFactory defaultAiFactory;
        private readonly GuardAiFactory guardAiFactory;
        private readonly GuardWaypointAiFactory guardWaypointAiFactory;
        private readonly PetAiFactory petAiFactory;
        private readonly StandStillAiFactory standStillAiFactory;
        private readonly WaypointAiFactory waypointAiFactory;
        public int ID;
        public WS_Creatures_AI.TBaseAI aiScript;
        public float SpawnX;
        public float SpawnY;
        public float SpawnZ;
        public float SpawnO;
        public short Faction;
        public float SpawnRange;
        public byte MoveType;
        public int MoveFlags;
        public byte cStandState;
        public Timer ExpireTimer;
        public int SpawnTime;
        public float SpeedMod;
        public int EquipmentID;
        public int WaypointID;
        public int GameEvent;
        public WS_Spells.CastSpellParameters SpellCasted;
        public bool DestroyAtNoCombat;
        public bool Flying;
        public int LastPercent;
        public float OldX;
        public float OldY;
        public float OldZ;
        public float MoveX;
        public float MoveY;
        public float MoveZ;
        public int LastMove;
        public int LastMove_Time;
        public bool PositionUpdated;
        private bool _disposedValue;

        public CreatureInfo CreatureInfo => worldState.CreaturesDatabase[ID];
        public string Name => CreatureInfo.Name;
        public float MaxDistance => 35f;

        public CreatureObject(
            ILogger<CreatureObject> logger,
            IScriptExecutor scriptExecutor,
            WorldState worldState,
            MangosConfiguration configuration,
            WS_DBCDatabase database,
            WS_Maps maps,
            WS_Creatures creatures,
            WS_Combat combat,
            WS_Loot loot,
            WS_Network network,
            IMapTileLoader tileLoader,
            LootObjectFactory lootObjectFactory,
            CreatureInfoFactory creatureInfoFactory,
            BaseActiveSpellFactory baseActiveSpellFactory,
            SpellTargetsFactory spellTargetsFactory,
            CastSpellParametersFactory castSpellParametersFactory,
            UpdateClassFactory updateClassFactory,
            CritterAiFactory critterAiFactory,
            DefaultAiFactory defaultAiFactory,
            GuardAiFactory guardAiFactory,
            GuardWaypointAiFactory guardWaypointAiFactory,
            PetAiFactory petAiFactory,
            StandStillAiFactory standStillAiFactory,
            WaypointAiFactory waypointAiFactory)
            : base(logger, worldState, null, baseActiveSpellFactory, updateClassFactory)
        {
            this.scriptExecutor = scriptExecutor;
            this.configuration = configuration;
            this.database = database;
            this.maps = maps;
            this.creatures = creatures;
            this.combat = combat;
            this.loot = loot;
            this.network = network;
            this.tileLoader = tileLoader;
            this.lootObjectFactory = lootObjectFactory;
            this.creatureInfoFactory = creatureInfoFactory;
            this.spellTargetsFactory = spellTargetsFactory;
            this.castSpellParametersFactory = castSpellParametersFactory;
            this.updateClassFactory = updateClassFactory;
            this.critterAiFactory = critterAiFactory;
            this.defaultAiFactory = defaultAiFactory;
            this.guardAiFactory = guardAiFactory;
            this.guardWaypointAiFactory = guardWaypointAiFactory;
            this.petAiFactory = petAiFactory;
            this.standStillAiFactory = standStillAiFactory;
            this.waypointAiFactory = waypointAiFactory;
        }

        public CreatureObject(
            ILogger<CreatureObject> logger,
            IScriptExecutor scriptExecutor,
            WorldState worldState,
            MangosConfiguration configuration,
            WS_DBCDatabase database,
            WS_Maps maps,
            WS_Creatures creatures,
            WS_Combat combat,
            WS_Loot loot,
            WS_Network network,
            IMapTileLoader tileLoader,
            LootObjectFactory lootObjectFactory,
            CreatureInfoFactory creatureInfoFactory,
            BaseActiveSpellFactory baseActiveSpellFactory,
            SpellTargetsFactory spellTargetsFactory,
            CastSpellParametersFactory castSpellParametersFactory,
            UpdateClassFactory updateClassFactory,
            CritterAiFactory critterAiFactory,
            DefaultAiFactory defaultAiFactory,
            GuardAiFactory guardAiFactory,
            GuardWaypointAiFactory guardWaypointAiFactory,
            PetAiFactory petAiFactory,
            StandStillAiFactory standStillAiFactory,
            WaypointAiFactory waypointAiFactory,
            ulong GUID_,
            DataRow infoRow = null)
        : this(
              logger,
              scriptExecutor,
              worldState,
              configuration,
              database,
              maps,
              creatures,
              combat,
              loot,
              network,
              tileLoader,
              lootObjectFactory,
              creatureInfoFactory,
              baseActiveSpellFactory,
              spellTargetsFactory,
              castSpellParametersFactory,
              updateClassFactory,
              critterAiFactory,
              defaultAiFactory,
              guardAiFactory,
              guardWaypointAiFactory,
              petAiFactory,
              standStillAiFactory,
              waypointAiFactory)
        {
            ID = 0;
            aiScript = null;
            SpawnX = 0f;
            SpawnY = 0f;
            SpawnZ = 0f;
            SpawnO = 0f;
            Faction = 0;
            SpawnRange = 0f;
            MoveType = 0;
            MoveFlags = 0;
            cStandState = 0;
            ExpireTimer = null;
            SpawnTime = 0;
            SpeedMod = 1f;
            EquipmentID = 0;
            WaypointID = 0;
            GameEvent = 0;
            SpellCasted = null;
            DestroyAtNoCombat = false;
            Flying = false;
            LastPercent = 100;
            OldX = 0f;
            OldY = 0f;
            OldZ = 0f;
            MoveX = 0f;
            MoveY = 0f;
            MoveZ = 0f;
            LastMove = 0;
            LastMove_Time = 0;
            PositionUpdated = true;
            if (infoRow == null)
            {
                DataTable MySQLQuery = new();
                worldDatabase.Query($"SELECT * FROM creature LEFT OUTER JOIN game_event_creature ON creature.guid = game_event_creature.guid WHERE creature.guid = {GUID_};", ref MySQLQuery);
                if (MySQLQuery.Rows.Count <= 0)
                {
                    logger.LogError("Creature Spawn not found in database. [GUID={0:X}]", GUID_);
                    return;
                }
                infoRow = MySQLQuery.Rows[0];
            }
            DataRow row = null;
            DataTable AddonInfoQuery = new();
            worldDatabase.Query($"SELECT * FROM spawns_creatures_addon WHERE spawn_id = {GUID_};", ref AddonInfoQuery);
            if (AddonInfoQuery.Rows.Count > 0)
            {
                row = AddonInfoQuery.Rows[0];
            }
            positionX = infoRow.As<float>("position_X");
            positionY = infoRow.As<float>("position_Y");
            positionZ = infoRow.As<float>("position_Z");
            orientation = infoRow.As<float>("orientation");
            OldX = positionX;
            OldY = positionY;
            OldZ = positionZ;
            SpawnX = positionX;
            SpawnY = positionY;
            SpawnZ = positionZ;
            SpawnO = orientation;
            ID = infoRow.As<int>("id");
            MapID = infoRow.As<uint>("map");
            SpawnID = infoRow.As<int>("guid");
            Model = infoRow.As<int>("modelid");
            SpawnTime = infoRow.As<int>("spawntimesecs");
            SpawnRange = infoRow.As<float>("spawndist");
            MoveType = infoRow.As<byte>("MovementType");
            Life.Current = infoRow.As<int>("curhealth");
            Mana.Current = infoRow.As<int>("curmana");
            EquipmentID = infoRow.As<int>("equipment_id");
            if (row != null)
            {
                Mount = row.As<int>("spawn_mount");
                cEmoteState = row.As<int>("spawn_emote");
                MoveFlags = row.As<int>("spawn_moveflags");
                cBytes0 = row.As<int>("spawn_bytes0");
                cBytes1 = row.As<int>("spawn_bytes1");
                cBytes2 = row.As<int>("spawn_bytes2");
                WaypointID = row.As<int>("spawn_pathid");
            }
            if (!worldState.CreaturesDatabase.ContainsKey(ID))
            {
                var baseCreature = creatureInfoFactory.Create(ID);
            }
            GUID = checked(GUID_ + MangosGlobalConstants.GUID_UNIT);
            Initialize();
            try
            {
                worldState.WorldCreaturesLock.EnterWriteLock();
                worldState.WorldCreatures.Add(GUID, this);
                worldState.WorldCreatureKeys.Add(GUID);
                worldState.WorldCreaturesLock.ExitWriteLock();
            }
            catch (Exception ex2)
            {
                ProjectData.SetProjectError(ex2);
                var ex = ex2;
                logger.LogWarning("WS_Creatures:New failed - Guid: {1}  {0}", ex.Message, GUID_);
                ProjectData.ClearProjectError();
            }
        }

        public CreatureObject(
            ILogger<CreatureObject> logger,
            IScriptExecutor scriptExecutor,
            WorldState worldState,
            MangosConfiguration configuration,
            WS_DBCDatabase database,
            WS_Maps maps,
            WS_Creatures creatures,
            WS_Combat combat,
            WS_Loot loot,
            WS_Network network,
            IMapTileLoader tileLoader,
            LootObjectFactory lootObjectFactory,
            CreatureInfoFactory creatureInfoFactory,
            BaseActiveSpellFactory baseActiveSpellFactory,
            SpellTargetsFactory spellTargetsFactory,
            CastSpellParametersFactory castSpellParametersFactory,
            UpdateClassFactory updateClassFactory,
            CritterAiFactory critterAiFactory,
            DefaultAiFactory defaultAiFactory,
            GuardAiFactory guardAiFactory,
            GuardWaypointAiFactory guardWaypointAiFactory,
            PetAiFactory petAiFactory,
            StandStillAiFactory standStillAiFactory,
            WaypointAiFactory waypointAiFactory,
            ulong GUID_,
            int ID_)
        : this(
              logger,
              scriptExecutor,
              worldState,
              configuration,
              database,
              maps,
              creatures,
              combat,
              loot,
              network,
              tileLoader,
              lootObjectFactory,
              creatureInfoFactory,
              baseActiveSpellFactory,
              spellTargetsFactory,
              castSpellParametersFactory,
              updateClassFactory,
              critterAiFactory,
              defaultAiFactory,
              guardAiFactory,
              guardWaypointAiFactory,
              petAiFactory,
              standStillAiFactory,
              waypointAiFactory)
        {
            ID = 0;
            aiScript = null;
            SpawnX = 0f;
            SpawnY = 0f;
            SpawnZ = 0f;
            SpawnO = 0f;
            Faction = 0;
            SpawnRange = 0f;
            MoveType = 0;
            MoveFlags = 0;
            cStandState = 0;
            ExpireTimer = null;
            SpawnTime = 0;
            SpeedMod = 1f;
            EquipmentID = 0;
            WaypointID = 0;
            GameEvent = 0;
            SpellCasted = null;
            DestroyAtNoCombat = false;
            Flying = false;
            LastPercent = 100;
            OldX = 0f;
            OldY = 0f;
            OldZ = 0f;
            MoveX = 0f;
            MoveY = 0f;
            MoveZ = 0f;
            LastMove = 0;
            LastMove_Time = 0;
            PositionUpdated = true;
            if (!worldState.CreaturesDatabase.ContainsKey(ID_))
            {
                var baseCreature = creatureInfoFactory.Create(ID_);
            }

            this.maps = maps;
            ID = ID_;
            GUID = GUID_;
            Initialize();
            try
            {
                worldState.WorldCreaturesLock.EnterWriteLock();
                worldState.WorldCreatures.Add(GUID, this);
                worldState.WorldCreatureKeys.Add(GUID);
                worldState.WorldCreaturesLock.ExitWriteLock();
            }
            catch (Exception ex2)
            {
                ProjectData.SetProjectError(ex2);
                var ex = ex2;
                logger.LogWarning("WS_Creatures:New failed - Guid: {1} ID: {2}  {0}", ex.Message, GUID_, ID_);
                ProjectData.ClearProjectError();
            }
        }

        public CreatureObject(
            ILogger<CreatureObject> logger,
            IScriptExecutor scriptExecutor,
            WorldState worldState,
            MangosConfiguration configuration,
            WS_DBCDatabase database,
            WS_Maps maps,
            WS_Creatures creatures,
            WS_Combat combat,
            WS_Loot loot,
            WS_Network network,
            IMapTileLoader tileLoader,
            LootObjectFactory lootObjectFactory,
            CreatureInfoFactory creatureInfoFactory,
            BaseActiveSpellFactory baseActiveSpellFactory,
            SpellTargetsFactory spellTargetsFactory,
            CastSpellParametersFactory castSpellParametersFactory,
            UpdateClassFactory updateClassFactory,
            CritterAiFactory critterAiFactory,
            DefaultAiFactory defaultAiFactory,
            GuardAiFactory guardAiFactory,
            GuardWaypointAiFactory guardWaypointAiFactory,
            PetAiFactory petAiFactory,
            StandStillAiFactory standStillAiFactory,
            WaypointAiFactory waypointAiFactory,
            int ID_)
        : this(
            logger,
            scriptExecutor,
            worldState,
            configuration,
            database,
            maps,
            creatures,
            combat,
            loot,
            network,
            tileLoader,
            lootObjectFactory,
            creatureInfoFactory,
            baseActiveSpellFactory,
            spellTargetsFactory,
            castSpellParametersFactory,
            updateClassFactory,
            critterAiFactory,
            defaultAiFactory,
            guardAiFactory,
            guardWaypointAiFactory,
            petAiFactory,
            standStillAiFactory,
            waypointAiFactory)
        {
            ID = 0;
            aiScript = null;
            SpawnX = 0f;
            SpawnY = 0f;
            SpawnZ = 0f;
            SpawnO = 0f;
            Faction = 0;
            SpawnRange = 0f;
            MoveType = 0;
            MoveFlags = 0;
            cStandState = 0;
            ExpireTimer = null;
            SpawnTime = 0;
            SpeedMod = 1f;
            EquipmentID = 0;
            WaypointID = 0;
            GameEvent = 0;
            SpellCasted = null;
            DestroyAtNoCombat = false;
            Flying = false;
            LastPercent = 100;
            OldX = 0f;
            OldY = 0f;
            OldZ = 0f;
            MoveX = 0f;
            MoveY = 0f;
            MoveZ = 0f;
            LastMove = 0;
            LastMove_Time = 0;
            PositionUpdated = true;
            if (!worldState.CreaturesDatabase.ContainsKey(ID_))
            {
                var baseCreature = creatureInfoFactory.Create(ID_);
            }

            this.maps = maps;
            ID = ID_;
            GUID = creatures.GetNewGUID();
            Initialize();
            try
            {
                worldState.WorldCreaturesLock.EnterWriteLock();
                worldState.WorldCreatures.Add(GUID, this);
                worldState.WorldCreatureKeys.Add(GUID);
                worldState.WorldCreaturesLock.ExitWriteLock();
            }
            catch (Exception ex2)
            {
                ProjectData.SetProjectError(ex2);
                var ex = ex2;
                logger.LogWarning("WS_Creatures:New failed - Guid: {1} ID: {2}  {0}", ex.Message, ID, ID_);
                ProjectData.ClearProjectError();
            }
        }

        public CreatureObject(
            ILogger<CreatureObject> logger,
            IScriptExecutor scriptExecutor,
            WorldState worldState,
            MangosConfiguration configuration,
            WS_DBCDatabase database,
            WS_Maps maps,
            WS_Creatures creatures,
            WS_Combat combat,
            WS_Loot loot,
            WS_Network network,
            IMapTileLoader tileLoader,
            LootObjectFactory lootObjectFactory,
            CreatureInfoFactory creatureInfoFactory,
            BaseActiveSpellFactory baseActiveSpellFactory,
            SpellTargetsFactory spellTargetsFactory,
            CastSpellParametersFactory castSpellParametersFactory,
            UpdateClassFactory updateClassFactory,
            CritterAiFactory critterAiFactory,
            DefaultAiFactory defaultAiFactory,
            GuardAiFactory guardAiFactory,
            GuardWaypointAiFactory guardWaypointAiFactory,
            PetAiFactory petAiFactory,
            StandStillAiFactory standStillAiFactory,
            WaypointAiFactory waypointAiFactory,
            int ID_,
            float PosX,
            float PosY,
            float PosZ,
            float Orientation_,
            int Map,
            int Duration = 0)
        : this(
            logger,
            scriptExecutor,
            worldState,
            configuration,
            database,
            maps,
            creatures,
            combat,
            loot,
            network,
            tileLoader,
            lootObjectFactory,
            creatureInfoFactory,
            baseActiveSpellFactory,
            spellTargetsFactory,
            castSpellParametersFactory,
            updateClassFactory,
            critterAiFactory,
            defaultAiFactory,
            guardAiFactory,
            guardWaypointAiFactory,
            petAiFactory,
            standStillAiFactory,
            waypointAiFactory)
        {
            ID = 0;
            aiScript = null;
            SpawnX = 0f;
            SpawnY = 0f;
            SpawnZ = 0f;
            SpawnO = 0f;
            Faction = 0;
            SpawnRange = 0f;
            MoveType = 0;
            MoveFlags = 0;
            cStandState = 0;
            ExpireTimer = null;
            SpawnTime = 0;
            SpeedMod = 1f;
            EquipmentID = 0;
            WaypointID = 0;
            GameEvent = 0;
            SpellCasted = null;
            DestroyAtNoCombat = false;
            Flying = false;
            LastPercent = 100;
            OldX = 0f;
            OldY = 0f;
            OldZ = 0f;
            MoveX = 0f;
            MoveY = 0f;
            MoveZ = 0f;
            LastMove = 0;
            LastMove_Time = 0;
            PositionUpdated = true;
            if (!worldState.CreaturesDatabase.ContainsKey(ID_))
            {
                var baseCreature = creatureInfoFactory.Create(ID_);
            }

            ID = ID_;
            GUID = creatures.GetNewGUID();
            positionX = PosX;
            positionY = PosY;
            positionZ = PosZ;
            orientation = Orientation_;
            MapID = checked((uint)Map);
            SpawnX = PosX;
            SpawnY = PosY;
            SpawnZ = PosZ;
            SpawnO = Orientation_;
            Initialize();
            if (Duration > 0)
            {
                ExpireTimer = new Timer(Destroy, null, Duration, Duration);
            }
            try
            {
                worldState.WorldCreaturesLock.EnterWriteLock();
                worldState.WorldCreatures.Add(GUID, this);
                worldState.WorldCreatureKeys.Add(GUID);
                worldState.WorldCreaturesLock.ExitWriteLock();
            }
            catch (Exception ex2)
            {
                ProjectData.SetProjectError(ex2);
                var ex = ex2;
                logger.LogWarning("WS_Creatures:New failed - Guid: {1} ID: {2} Map: {3}  {0}", ex.Message, GUID, ID_, Map);
                ProjectData.ClearProjectError();
            }
        }

        // TODO: replace magic numbers
        public bool IsAbleToWalkOnWater => CreatureInfo.CreatureFamily switch
        {
            3 or 10 or 11 or 12 or 20 or 21 or 27 => false,
            _ => true,
        };

        public bool IsAbleToWalkOnGround
        {
            get
            {
                var creatureFamily = CreatureInfo.CreatureFamily;
                return creatureFamily != byte.MaxValue;
            }
        }

        public bool IsCritter => CreatureInfo.CreatureType == 8;

        public bool IsGuard => (CreatureInfo.cNpcFlags & 0x40) == 64;

        public override bool IsDead => aiScript != null
                    ? Life.Current == 0 || aiScript.State == AIState.AI_DEAD || aiScript.State == AIState.AI_RESPAWN
                    : Life.Current == 0;

        public bool Evade => aiScript != null && aiScript.State == AIState.AI_MOVING_TO_SPAWN;

        public int NPCTextID
        {
            get
            {
                checked
                {
                    return database.CreatureGossip.ContainsKey(GUID - MangosGlobalConstants.GUID_UNIT)
                        ? database.CreatureGossip[GUID - MangosGlobalConstants.GUID_UNIT]
                        : 16777215;
                }
            }
        }

        public override bool IsFriendlyTo(ref WS_Base.BaseUnit Unit)
        {
            if (Unit == this)
            {
                return true;
            }
            if (Unit is CharacterObject characterObject)
            {
                if (characterObject.GM)
                {
                    return true;
                }
                if (characterObject.GetReputation(characterObject.Faction) < ReputationRank.Friendly)
                {
                    return false;
                }
                if (characterObject.GetReaction(characterObject.Faction) < TReaction.NEUTRAL)
                {
                    return false;
                }
            }
            else if (Unit is CreatureObject)
            {
            }
            return true;
        }

        public override bool IsEnemyTo(ref WS_Base.BaseUnit Unit)
        {
            if (Unit == this)
            {
                return false;
            }
            if (Unit is CharacterObject characterObject)
            {
                if (characterObject.GM)
                {
                    return false;
                }
                if (characterObject.GetReputation(characterObject.Faction) < ReputationRank.Friendly)
                {
                    return true;
                }
                if (characterObject.GetReaction(characterObject.Faction) < TReaction.NEUTRAL)
                {
                    return true;
                }
            }
            else if (Unit is CreatureObject)
            {
            }
            return false;
        }

        public float AggroRange(CharacterObject objCharacter)
        {
            checked
            {
                var LevelDiff = (short)unchecked(Level - objCharacter.Level);
                float Range = 20 + LevelDiff;
                if (Range < 5f)
                {
                    Range = 5f;
                }
                if (Range > 45f)
                {
                    Range = 45f;
                }
                return Range;
            }
        }

        public void SendTargetUpdate(ulong TargetGUID)
        {
            Packets.UpdatePacketClass packet = new();
            var tmpUpdate = updateClassFactory.Create(188);
            tmpUpdate.SetUpdateFlag(16, TargetGUID);
            Packets.PacketClass packet2 = packet;
            var updateObject = this;
            tmpUpdate.AddToPacket(ref packet2, ObjectUpdateType.UPDATETYPE_VALUES, ref updateObject);
            tmpUpdate.Dispose();
            packet2 = packet;
            SendToNearPlayers(ref packet2);
            packet.Dispose();
        }

        public WS_Base.BaseUnit GetRandomTarget()
        {
            if (aiScript == null)
            {
                return null;
            }
            if (aiScript.aiHateTable.Count == 0)
            {
                return null;
            }
            var i = 0;
            var target = WorldState.Rnd.Next(0, aiScript.aiHateTable.Count);
            foreach (var tmpUnit in aiScript.aiHateTable)
            {
                if (target == i)
                {
                    return tmpUnit.Key;
                }
                i = checked(i + 1);
            }
            return null;
        }

        public void FillAllUpdateFlags(ref Packets.UpdateClass Update)
        {
            Update.SetUpdateFlag(0, GUID);
            Update.SetUpdateFlag(4, Size);
            Update.SetUpdateFlag(2, 9);
            Update.SetUpdateFlag(3, ID);
            if (aiScript != null && aiScript.aiTarget != null)
            {
                Update.SetUpdateFlag(16, aiScript.aiTarget.GUID);
            }
            if (decimal.Compare(new decimal(SummonedBy), 0m) > 0)
            {
                Update.SetUpdateFlag(12, SummonedBy);
            }
            if (decimal.Compare(new decimal(CreatedBy), 0m) > 0)
            {
                Update.SetUpdateFlag(14, CreatedBy);
            }
            if (CreatedBySpell > 0)
            {
                Update.SetUpdateFlag(146, CreatedBySpell);
            }
            Update.SetUpdateFlag(131, Model);
            Update.SetUpdateFlag(132, worldState.CreaturesDatabase[ID].GetFirstModel);
            if (Mount > 0)
            {
                Update.SetUpdateFlag(133, Mount);
            }
            Update.SetUpdateFlag(36, cBytes0);
            Update.SetUpdateFlag(138, cBytes1);
            Update.SetUpdateFlag(164, cBytes2);
            Update.SetUpdateFlag(148, cEmoteState);
            Update.SetUpdateFlag(22, Life.Current);
            checked
            {
                Update.SetUpdateFlag(23 + worldState.CreaturesDatabase[ID].ManaType, Mana.Current);
                Update.SetUpdateFlag(28, Life.Maximum);
                Update.SetUpdateFlag(29 + worldState.CreaturesDatabase[ID].ManaType, Mana.Maximum);
                Update.SetUpdateFlag(34, Level);
                Update.SetUpdateFlag(35, Faction);
                Update.SetUpdateFlag(147, worldState.CreaturesDatabase[ID].cNpcFlags);
                Update.SetUpdateFlag(46, cUnitFlags);
                Update.SetUpdateFlag(143, cDynamicFlags);
                Update.SetUpdateFlag(155, worldState.CreaturesDatabase[ID].Resistances[0]);
                Update.SetUpdateFlag(156, worldState.CreaturesDatabase[ID].Resistances[1]);
                Update.SetUpdateFlag(157, worldState.CreaturesDatabase[ID].Resistances[2]);
                Update.SetUpdateFlag(158, worldState.CreaturesDatabase[ID].Resistances[3]);
                Update.SetUpdateFlag(159, worldState.CreaturesDatabase[ID].Resistances[4]);
                Update.SetUpdateFlag(160, worldState.CreaturesDatabase[ID].Resistances[5]);
                Update.SetUpdateFlag(161, worldState.CreaturesDatabase[ID].Resistances[6]);
                if (EquipmentID > 0)
                {
                    try
                    {
                        if (database.CreatureEquip.ContainsKey(EquipmentID))
                        {
                            var EquipmentInfo = database.CreatureEquip[EquipmentID];
                            Update.SetUpdateFlag(37, EquipmentInfo.EquipModel[0]);
                            Update.SetUpdateFlag(40, EquipmentInfo.EquipInfo[0]);
                            Update.SetUpdateFlag(41, EquipmentInfo.EquipSlot[0]);
                            Update.SetUpdateFlag(38, EquipmentInfo.EquipModel[1]);
                            Update.SetUpdateFlag(42, EquipmentInfo.EquipInfo[1]);
                            Update.SetUpdateFlag(43, EquipmentInfo.EquipSlot[1]);
                            Update.SetUpdateFlag(39, EquipmentInfo.EquipModel[2]);
                            Update.SetUpdateFlag(44, EquipmentInfo.EquipInfo[2]);
                            Update.SetUpdateFlag(45, EquipmentInfo.EquipSlot[2]);
                        }
                    }
                    catch (DataException ex2)
                    {
                        ProjectData.SetProjectError(ex2);
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"FillAllUpdateFlags : Unable to equip items {EquipmentID} for Creature");
                        Console.ForegroundColor = ConsoleColor.Gray;
                        ProjectData.ClearProjectError();
                    }
                }
                Update.SetUpdateFlag(129, BoundingRadius);
                Update.SetUpdateFlag(130, CombatReach);
                var num = MangosGlobalConstants.MAX_AURA_EFFECTs_VISIBLE - 1;
                for (var k = 0; k <= num; k++)
                {
                    if (ActiveSpells[k] != null)
                    {
                        Update.SetUpdateFlag(47 + k, ActiveSpells[k].SpellID);
                    }
                }
                var num2 = MangosGlobalConstants.MAX_AURA_EFFECT_FLAGs - 1;
                for (var j = 0; j <= num2; j++)
                {
                    Update.SetUpdateFlag(95 + j, ActiveSpells_Flags[j]);
                }
                var num3 = MangosGlobalConstants.MAX_AURA_EFFECT_LEVELSs - 1;
                for (var i = 0; i <= num3; i++)
                {
                    Update.SetUpdateFlag(113 + i, ActiveSpells_Count[i]);
                    Update.SetUpdateFlag(101 + i, ActiveSpells_Level[i]);
                }
            }
        }

        public void MoveToInstant(float x, float y, float z, float o)
        {
            positionX = x;
            positionY = y;
            positionZ = z;
            orientation = o;
            if (SeenBy.Count > 0)
            {
                Packets.PacketClass packet = new(Opcodes.MSG_MOVE_HEARTBEAT);
                packet.AddPackGUID(GUID);
                packet.AddInt32(0);
                packet.AddInt32(LegacyNativeMethods.TimeGetTime(""));
                packet.AddSingle(positionX);
                packet.AddSingle(positionY);
                packet.AddSingle(positionZ);
                packet.AddSingle(orientation);
                packet.AddInt32(0);
                SendToNearPlayers(ref packet);
                packet.Dispose();
            }
        }

        public void SetToRealPosition(bool Forced = false)
        {
            if (aiScript != null && (Forced || aiScript.State != AIState.AI_MOVING_TO_SPAWN))
            {
                var timeDiff = checked(LegacyNativeMethods.TimeGetTime("") - LastMove);
                if ((Forced || aiScript.IsMoving) && LastMove > 0 && timeDiff < LastMove_Time)
                {
                    var distance = (aiScript.State is not AIState.AI_MOVING and not AIState.AI_WANDERING) ? (timeDiff / 1000f * (CreatureInfo.RunSpeed * SpeedMod)) : (timeDiff / 1000f * (CreatureInfo.WalkSpeed * SpeedMod));
                    positionX = (float)(OldX + (Math.Cos(orientation) * distance));
                    positionY = (float)(OldY + (Math.Sin(orientation) * distance));
                    positionZ = maps.GetZCoord(positionX, positionY, positionZ, MapID);
                }
                else if (!PositionUpdated && timeDiff >= LastMove_Time)
                {
                    PositionUpdated = true;
                    positionX = MoveX;
                    positionY = MoveY;
                    positionZ = MoveZ;
                }
            }
        }

        public void StopMoving()
        {
            if (aiScript != null && !aiScript.InCombat)
            {
                aiScript.Pause(10000);
                SetToRealPosition(Forced: true);
                MoveToInstant(positionX, positionY, positionZ, orientation);
            }
        }

        public int MoveTo(float x, float y, float z, float o = 0f, bool Running = false)
        {
            try
            {
                if (SeenBy.Count == 0)
                {
                    return 10000;
                }
            }
            catch (Exception projectError)
            {
                ProjectData.SetProjectError(projectError);
                baseLogger.LogWarning("MoveTo:SeenBy Failed");
                ProjectData.ClearProjectError();
            }
            var TimeToMove = 1;
            Packets.PacketClass SMSG_MONSTER_MOVE = new(Opcodes.SMSG_MONSTER_MOVE);
            checked
            {
                try
                {
                    SMSG_MONSTER_MOVE.AddPackGUID(GUID);
                    SMSG_MONSTER_MOVE.AddSingle(positionX);
                    SMSG_MONSTER_MOVE.AddSingle(positionY);
                    SMSG_MONSTER_MOVE.AddSingle(positionZ);
                    SMSG_MONSTER_MOVE.AddInt32(network.MsTime());
                    if (o == 0f)
                    {
                        SMSG_MONSTER_MOVE.AddInt8(0);
                    }
                    else
                    {
                        SMSG_MONSTER_MOVE.AddInt8(4);
                        SMSG_MONSTER_MOVE.AddSingle(o);
                    }
                    var moveDist = WS_Combat.GetDistance(positionX, x, positionY, y, positionZ, z);
                    if (Flying)
                    {
                        SMSG_MONSTER_MOVE.AddInt32(768);
                        TimeToMove = (int)Math.Round((moveDist / (CreatureInfo.RunSpeed * SpeedMod) * 1000f) + 0.5f);
                    }
                    else if (Running)
                    {
                        SMSG_MONSTER_MOVE.AddInt32(256);
                        TimeToMove = (int)Math.Round((moveDist / (CreatureInfo.RunSpeed * SpeedMod) * 1000f) + 0.5f);
                    }
                    else
                    {
                        SMSG_MONSTER_MOVE.AddInt32(0);
                        TimeToMove = (int)Math.Round((moveDist / (CreatureInfo.WalkSpeed * SpeedMod) * 1000f) + 0.5f);
                    }
                    orientation = combat.GetOrientation(positionX, x, positionY, y);
                    OldX = positionX;
                    OldY = positionY;
                    OldZ = positionZ;
                    LastMove = LegacyNativeMethods.TimeGetTime("");
                    LastMove_Time = TimeToMove;
                    PositionUpdated = false;
                    positionX = x;
                    positionY = y;
                    positionZ = z;
                    MoveX = x;
                    MoveY = y;
                    MoveZ = z;
                    SMSG_MONSTER_MOVE.AddInt32(TimeToMove);
                    SMSG_MONSTER_MOVE.AddInt32(1);
                    SMSG_MONSTER_MOVE.AddSingle(x);
                    SMSG_MONSTER_MOVE.AddSingle(y);
                    SMSG_MONSTER_MOVE.AddSingle(z);
                    SendToNearPlayers(ref SMSG_MONSTER_MOVE);
                }
                catch (Exception ex2)
                {
                    ProjectData.SetProjectError(ex2);
                    var ex = ex2;
                    baseLogger.LogWarning("MoveTo:Main Failed - {0}", ex.Message);
                    ProjectData.ClearProjectError();
                }
                finally
                {
                    SMSG_MONSTER_MOVE.Dispose();
                }
                MoveCell();
                return TimeToMove;
            }
        }

        public bool CanMoveTo(float x, float y, float z)
        {
            var wS_Maps = maps;
            WS_Base.BaseObject objCharacter = this;
            if (wS_Maps.IsOutsideOfMap(ref objCharacter))
            {
                return false;
            }
            if (z < maps.GetWaterLevel(x, y, checked((int)MapID)))
            {
                if (!IsAbleToWalkOnWater)
                {
                    return false;
                }
            }
            else if (!IsAbleToWalkOnGround)
            {
                return false;
            }
            return true;
        }

        public void TurnTo(ref WS_Base.BaseObject Target)
        {
            TurnTo(Target.positionX, Target.positionY);
        }

        public void TurnTo(float x, float y)
        {
            orientation = combat.GetOrientation(positionX, x, positionY, y);
            TurnTo(orientation);
        }

        public void TurnTo(float orientation_)
        {
            orientation = orientation_;
            if (SeenBy.Count > 0 && (aiScript == null || !aiScript.IsMoving))
            {
                Packets.PacketClass packet = new(Opcodes.MSG_MOVE_HEARTBEAT);
                try
                {
                    packet.AddPackGUID(GUID);
                    packet.AddInt32(0);
                    packet.AddInt32(LegacyNativeMethods.TimeGetTime(""));
                    packet.AddSingle(positionX);
                    packet.AddSingle(positionY);
                    packet.AddSingle(positionZ);
                    packet.AddSingle(orientation);
                    packet.AddInt32(0);
                    SendToNearPlayers(ref packet);
                }
                finally
                {
                    packet.Dispose();
                }
            }
        }

        public override void Die(ref WS_Base.BaseUnit Attacker)
        {
            cUnitFlags = 16384;
            Life.Current = 0;
            Mana.Current = 0;
            if (aiScript != null)
            {
                SetToRealPosition(Forced: true);
                MoveToInstant(positionX, positionY, positionZ, orientation);
                PositionUpdated = true;
                LastMove = 0;
                LastMove_Time = 0;
                aiScript.State = AIState.AI_DEAD;
                aiScript.DoThink();
            }
            aiScript?.OnDeath();
            if (Attacker != null && Attacker is CreatureObject @object && @object.aiScript != null)
            {
                var tBaseAI = @object.aiScript;
                WS_Base.BaseUnit Victim = this;
                tBaseAI.OnKill(ref Victim);
            }
            Packets.UpdatePacketClass packetForNear = new();
            var UpdateData = updateClassFactory.Create(188);
            checked
            {
                var num = MangosGlobalConstants.MAX_AURA_EFFECTs_VISIBLE - 1;
                for (var i = 0; i <= num; i++)
                {
                    if (ActiveSpells[i] != null)
                    {
                        RemoveAura(i, ref ActiveSpells[i].SpellCaster, RemovedByDuration: false, SendUpdate: false);
                        UpdateData.SetUpdateFlag(47 + i, 0);
                    }
                }
                UpdateData.SetUpdateFlag(22, Life.Current);
            }
            UpdateData.SetUpdateFlag((int)checked(23 + base.ManaType), Mana.Current);
            UpdateData.SetUpdateFlag(46, cUnitFlags);
            Packets.PacketClass packet = packetForNear;
            var updateObject = this;
            UpdateData.AddToPacket(ref packet, ObjectUpdateType.UPDATETYPE_VALUES, ref updateObject);
            packet = packetForNear;
            SendToNearPlayers(ref packet);
            packetForNear = (Packets.UpdatePacketClass)packet;
            packetForNear.Dispose();
            UpdateData.Dispose();
            if (Attacker is CharacterObject object1)
            {
                object1.RemoveFromCombat(this);
                CharacterObject Character;
                if (!IsCritter && !IsGuard && CreatureInfo.cNpcFlags == 0)
                {
                    Character = object1;
                    GiveXP(ref Character);
                    Character = object1;
                    LootCorpse(ref Character);
                }
                var aLLQUESTS = worldState.QuestsService;
                Character = object1;
                updateObject = this;
                aLLQUESTS.OnQuestKill(ref Character, ref updateObject);
                Attacker = Character;
            }
        }

        public override void DealDamage(int Damage, WS_Base.BaseUnit Attacker = null)
        {
            if (Life.Current == 0)
            {
                return;
            }
            RemoveAurasByInterruptFlag(2);
            checked
            {
                Life.Current -= Damage;
                if (Attacker != null && aiScript != null)
                {
                    aiScript.OnGenerateHate(ref Attacker, Damage);
                }
                if (Life.Current == 0)
                {
                    Die(ref Attacker);
                    return;
                }
                var tmpPercent = (int)(Life.Current / (double)Life.Maximum * 100.0);
                if (tmpPercent != LastPercent)
                {
                    LastPercent = tmpPercent;
                    aiScript?.OnHealthChange(LastPercent);
                }
            }
            if (SeenBy.Count > 0)
            {
                Packets.UpdatePacketClass packetForNear = new();
                var UpdateData = updateClassFactory.Create(188);
                UpdateData.SetUpdateFlag(22, Life.Current);
                UpdateData.SetUpdateFlag((int)checked(23 + base.ManaType), Mana.Current);
                Packets.PacketClass packet = packetForNear;
                var updateObject = this;
                UpdateData.AddToPacket(ref packet, ObjectUpdateType.UPDATETYPE_VALUES, ref updateObject);
                packet = packetForNear;
                SendToNearPlayers(ref packet);
                packetForNear.Dispose();
                UpdateData.Dispose();
            }
        }

        public override void Heal(int Damage, WS_Base.BaseUnit Attacker = null)
        {
            checked
            {
                if (Life.Current != 0)
                {
                    Life.Current += Damage;
                    if (SeenBy.Count > 0)
                    {
                        Packets.UpdatePacketClass packetForNear = new();
                        var UpdateData = updateClassFactory.Create(188);
                        UpdateData.SetUpdateFlag(22, Life.Current);
                        Packets.PacketClass packet = packetForNear;
                        var updateObject = this;
                        UpdateData.AddToPacket(ref packet, ObjectUpdateType.UPDATETYPE_VALUES, ref updateObject);
                        packet = packetForNear;
                        SendToNearPlayers(ref packet);
                        packetForNear.Dispose();
                        UpdateData.Dispose();
                    }
                }
            }
        }

        public override void Energize(int Damage, ManaTypes Power, WS_Base.BaseUnit Attacker = null)
        {
            if (ManaType == Power)
            {
                checked
                {
                    Mana.Current += Damage;
                }
                if (SeenBy.Count > 0)
                {
                    Packets.UpdatePacketClass packetForNear = new();
                    var UpdateData = updateClassFactory.Create(188);
                    UpdateData.SetUpdateFlag((int)checked(23 + base.ManaType), Mana.Current);
                    Packets.PacketClass packet = packetForNear;
                    var updateObject = this;
                    UpdateData.AddToPacket(ref packet, ObjectUpdateType.UPDATETYPE_VALUES, ref updateObject);
                    packet = packetForNear;
                    SendToNearPlayers(ref packet);
                    packetForNear.Dispose();
                    UpdateData.Dispose();
                }
            }
        }

        public void LootCorpse(ref CharacterObject Character)
        {
            if (GenerateLoot(ref Character, LootType.LOOTTYPE_CORPSE))
            {
                cDynamicFlags = 1;
            }
            else
            {
                if (CreatureInfo.SkinLootID <= 0)
                {
                    return;
                }
                cUnitFlags |= 67108864;
            }
            Packets.PacketClass packet = new(Opcodes.SMSG_UPDATE_OBJECT);
            packet.AddInt32(1);
            packet.AddInt8(0);
            var UpdateData = updateClassFactory.Create(MangosGlobalConstants.FIELD_MASK_SIZE_PLAYER);
            UpdateData.SetUpdateFlag(143, cDynamicFlags);
            UpdateData.SetUpdateFlag(46, cUnitFlags);
            var updateObject = this;
            UpdateData.AddToPacket(ref packet, ObjectUpdateType.UPDATETYPE_VALUES, ref updateObject);
            UpdateData.Dispose();
            if (!WS_Loot.LootTable.ContainsKey(GUID) && (cUnitFlags & 0x4000000) == 67108864)
            {
                SendToNearPlayers(ref packet);
            }
            else if (Character.IsInGroup)
            {
                WS_Loot.LootTable[GUID].LootOwner = 0uL;
                switch (Character.Group.LootMethod)
                {
                    case GroupLootMethod.LOOT_FREE_FOR_ALL:
                        foreach (var objCharacter in Character.Group.LocalMembers)
                        {
                            if (SeenBy.Contains(objCharacter))
                            {
                                WS_Loot.LootTable[GUID].LootOwner = objCharacter;
                                worldState.Characters[objCharacter].client.Send(ref packet);
                            }
                        }
                        break;

                    case GroupLootMethod.LOOT_MASTER:
                        if (Character.Group.LocalLootMaster == null)
                        {
                            WS_Loot.LootTable[GUID].LootOwner = Character.GUID;
                            Character.client.Send(ref packet);
                        }
                        else
                        {
                            WS_Loot.LootTable[GUID].LootOwner = Character.Group.LocalLootMaster.GUID;
                            Character.Group.LocalLootMaster.client.Send(ref packet);
                        }
                        break;

                    case GroupLootMethod.LOOT_ROUND_ROBIN:
                    case GroupLootMethod.LOOT_GROUP:
                    case GroupLootMethod.LOOT_NEED_BEFORE_GREED:
                        {
                            var cLooter = Character.Group.GetNextLooter();
                            while (!SeenBy.Contains(cLooter.GUID) && cLooter != Character)
                            {
                                cLooter = Character.Group.GetNextLooter();
                            }
                            WS_Loot.LootTable[GUID].LootOwner = cLooter.GUID;
                            cLooter.client.Send(ref packet);
                            break;
                        }
                }
            }
            else
            {
                WS_Loot.LootTable[GUID].LootOwner = Character.GUID;
                Character.client.Send(ref packet);
            }
            packet.Dispose();
        }

        public bool GenerateLoot(ref CharacterObject Character, LootType LootType)
        {
            if (CreatureInfo.LootID == 0)
            {
                return false;
            }
            var Loot = lootObjectFactory.Create(GUID, LootType);
            WS_Loot.LootTemplates_Creature.GetLoot(CreatureInfo.LootID)?.Process(ref Loot, 0);
            checked
            {
                if (LootType == LootType.LOOTTYPE_CORPSE && CreatureInfo.CreatureType == 7)
                {
                    Loot.Money = WorldState.Rnd.Next((int)CreatureInfo.MinGold, (int)(CreatureInfo.MaxGold + 1L));
                }
                Loot.LootOwner = Character.GUID;
                return true;
            }
        }

        public void GiveXP(ref CharacterObject Character)
        {
            checked
            {
                var XP = (Level * 5) + 45;
                var lvlDifference = Character.Level - Level;
                if (lvlDifference > 0)
                {
                    XP = (int)Math.Round(XP * (1.0 + (0.05 * (Level - Character.Level))));
                }
                else if (lvlDifference < 0)
                {
                    var GrayLevel = Character.Level switch
                    {
                        0 or 1 or 2 or 3 or 4 or 5 => 0,
                        6 or 7 or 8 or 9 or 10 or 11 or 12 or 13 or 14 or 15 or 16 or 17 or 18 or 19 or 20 or 21 or 22 or 23 or 24 or 25 or 26 or 27 or 28 or 29 or 30 or 31 or 32 or 33 or 34 or 35 or 36 or 37 or 38 or 39 => (byte)Math.Round(Character.Level - Math.Floor(Character.Level / 10.0) - 5.0),
                        40 or 41 or 42 or 43 or 44 or 45 or 46 or 47 or 48 or 49 or 50 or 51 or 52 or 53 or 54 or 55 or 56 or 57 or 58 or 59 => (byte)Math.Round(Character.Level - Math.Floor(Character.Level / 5.0) - 1.0),
                        _ => (byte)(Character.Level - 9),
                    };
                    if (Level > (uint)GrayLevel)
                    {
                        var ZD = Character.Level switch
                        {
                            0 or 1 or 2 or 3 or 4 or 5 or 6 or 7 => 5,
                            8 or 9 => 6,
                            10 or 11 => 7,
                            12 or 13 or 14 or 15 => 8,
                            16 or 17 or 18 or 19 => 9,
                            20 or 21 or 22 or 23 or 24 or 25 or 26 or 27 or 28 or 29 => 11,
                            30 or 31 or 32 or 33 or 34 or 35 or 36 or 37 or 38 or 39 => 12,
                            40 or 41 or 42 or 43 or 44 => 13,
                            45 or 46 or 47 or 48 or 49 => 14,
                            50 or 51 or 52 or 53 or 54 => 15,
                            55 or 56 or 57 or 58 or 59 => 16,
                            _ => 17,
                        };
                        XP = (int)Math.Round(XP * (1.0 - ((Character.Level - Level) / (double)ZD)));
                    }
                    else
                    {
                        XP = 0;
                    }
                }
                if (worldState.CreaturesDatabase[ID].Elite > 0)
                {
                    XP *= 2;
                }
                XP = (int)Math.Round(XP * configuration.World.XPRate);
                if (!Character.IsInGroup)
                {
                    var RestedXP2 = 0;
                    if (Character.RestBonus >= 0)
                    {
                        RestedXP2 = XP;
                        if (RestedXP2 > Character.RestBonus)
                        {
                            RestedXP2 = Character.RestBonus;
                        }
                        Character.RestBonus -= RestedXP2;
                        XP += RestedXP2;
                    }
                    Character.AddXP(XP, RestedXP2, GUID);
                    return;
                }
                XP = (int)Math.Round(XP / (double)Character.Group.GetMembersCount());
                var membersCount = Character.Group.GetMembersCount();
                XP = (membersCount <= 2) ? (XP * 1) : (membersCount switch
                {
                    3 => (int)Math.Round(XP * 1.166),
                    4 => (int)Math.Round(XP * 1.3),
                    _ => (int)Math.Round(XP * 1.4),
                });
                var baseLvl = 0;
                foreach (var Member2 in Character.Group.LocalMembers)
                {
                    var characterObject = worldState.Characters[Member2];
                    if (!characterObject.DEAD && Math.Sqrt(Math.Pow(positionX - positionX, 2.0) + Math.Pow(positionY - positionY, 2.0)) <= VisibleDistance)
                    {
                        baseLvl += Level;
                    }
                    characterObject = null;
                }
                foreach (var Member in Character.Group.LocalMembers)
                {
                    var characterObject2 = worldState.Characters[Member];
                    if (!characterObject2.DEAD && Math.Sqrt(Math.Pow(positionX - positionX, 2.0) + Math.Pow(positionY - positionY, 2.0)) <= VisibleDistance)
                    {
                        var tmpXP = XP;
                        var RestedXP = 0;
                        if (characterObject2.RestBonus >= 0)
                        {
                            RestedXP = tmpXP;
                            if (RestedXP > characterObject2.RestBonus)
                            {
                                RestedXP = characterObject2.RestBonus;
                            }
                            characterObject2.RestBonus -= RestedXP;
                            tmpXP += RestedXP;
                        }
                        tmpXP = (int)(tmpXP * Level / (double)baseLvl);
                        characterObject2.AddXP(tmpXP, RestedXP, GUID, LogIt: false);
                        characterObject2.LogXPGain(tmpXP, RestedXP, GUID, (float)((Character.Group.GetMembersCount() - 1) / 10.0));
                    }
                }
            }
        }

        public void StopCasting()
        {
            if (SpellCasted != null && !SpellCasted.Finished)
            {
                SpellCasted.StopCast();
            }
        }

        public void ApplySpell(int SpellID)
        {
            if (WS_Spells.SPELLs.ContainsKey(SpellID))
            {
                var t = spellTargetsFactory.Create();
                WS_Base.BaseUnit objCharacter = this;
                t.SetTarget_SELF(ref objCharacter);
                var spellInfo = WS_Spells.SPELLs[SpellID];
                WS_Base.BaseObject caster = this;
                spellInfo.Apply(ref caster, t);
            }
        }

        public int CastSpellOnSelf(int SpellID)
        {
            if (Spell_Silenced)
            {
                return -1;
            }
            var Targets = spellTargetsFactory.Create();
            var spellTargets = Targets;
            WS_Base.BaseUnit objCharacter = this;
            spellTargets.SetTarget_SELF(ref objCharacter);
            WS_Base.BaseObject Caster = this;
            var tmpSpell = castSpellParametersFactory.Create(ref Targets, ref Caster, SpellID);
            if (WS_Spells.SPELLs[SpellID].GetDuration > 0)
            {
                SpellCasted = tmpSpell;
            }
            ThreadPool.QueueUserWorkItem(tmpSpell.Cast);
            return WS_Spells.SPELLs[SpellID].GetCastTime;
        }

        public int CastSpell(int SpellID, WS_Base.BaseUnit Target)
        {
            if (Spell_Silenced)
            {
                return -1;
            }
            if (Target == null)
            {
                return -1;
            }
            if (WS_Combat.GetDistance(this, Target) > WS_Spells.SPELLs[SpellID].GetRange)
            {
                return -1;
            }
            var Targets = spellTargetsFactory.Create();
            Targets.SetTarget_UNIT(ref Target);
            WS_Base.BaseObject Caster = this;
            var tmpSpell = castSpellParametersFactory.Create(ref Targets, ref Caster, SpellID);
            if (WS_Spells.SPELLs[SpellID].GetDuration > 0)
            {
                SpellCasted = tmpSpell;
            }
            ThreadPool.QueueUserWorkItem(tmpSpell.Cast);
            return WS_Spells.SPELLs[SpellID].GetCastTime;
        }

        public int CastSpell(int SpellID, float x, float y, float z)
        {
            if (Spell_Silenced)
            {
                return -1;
            }
            if (WS_Combat.GetDistance(this, x, y, z) > WS_Spells.SPELLs[SpellID].GetRange)
            {
                return -1;
            }

            var Targets = spellTargetsFactory.Create();
            Targets.SetTarget_DESTINATIONLOCATION(x, y, z);
            WS_Base.BaseObject Caster = this;
            var tmpSpell = castSpellParametersFactory.Create(ref Targets, ref Caster, SpellID);
            if (WS_Spells.SPELLs[SpellID].GetDuration > 0)
            {
                SpellCasted = tmpSpell;
            }
            ThreadPool.QueueUserWorkItem(tmpSpell.Cast);
            return WS_Spells.SPELLs[SpellID].GetCastTime;
        }

        public void SpawnCreature(int Entry, float PosX, float PosY, float PosZ)
        {
            var tmpCreature = creatureObjectFactory.Create(Entry, PosX, PosY, PosZ, 0f, checked((int)MapID));
            tmpCreature.instance = instance;
            tmpCreature.DestroyAtNoCombat = true;

            tmpCreature.AddToWorld();
            tmpCreature.aiScript?.Dispose();
            tmpCreature.aiScript = defaultAiFactory.Create(ref tmpCreature);
            tmpCreature.aiScript.aiHateTable = aiScript.aiHateTable;
            tmpCreature.aiScript.OnEnterCombat();
            tmpCreature.aiScript.State = AIState.AI_ATTACKING;
            tmpCreature.aiScript.DoThink();
        }

        public void SendChatMessage(string Message, ChatMsg msgType, LANGUAGES msgLanguage, ulong SecondGUID = 0uL)
        {
            Packets.PacketClass packet = new(Opcodes.SMSG_MESSAGECHAT);
            byte flag = 0;
            packet.AddInt8(checked((byte)msgType));
            packet.AddInt32((int)msgLanguage);
            if ((uint)(msgType - 11) <= 2u)
            {
                packet.AddUInt64(GUID);
                packet.AddInt32(checked(Encoding.UTF8.GetByteCount(Name) + 1));
                packet.AddString(Name);
                packet.AddUInt64(SecondGUID);
            }
            else
            {
                baseLogger.LogWarning("Creature.SendChatMessage() must not handle this chat type!");
            }
            packet.AddInt32(checked(Encoding.UTF8.GetByteCount(Message) + 1));
            packet.AddString(Message);
            packet.AddInt8(flag);
            SendToNearPlayers(ref packet);
            packet.Dispose();
        }

        public void ResetAI()
        {
            aiScript.Dispose();
            var Creature = this;
            aiScript = defaultAiFactory.Create(ref Creature);
            MoveType = 1;
        }

        public void Initialize()
        {
            Level = checked((byte)WorldState.Rnd.Next(worldState.CreaturesDatabase[ID].LevelMin, worldState.CreaturesDatabase[ID].LevelMax));
            Size = worldState.CreaturesDatabase[ID].Size;
            if (Size == 0f)
            {
                Size = 1f;
            }
            Model = worldState.CreaturesDatabase[ID].GetRandomModel;
            ManaType = (ManaTypes)worldState.CreaturesDatabase[ID].ManaType;
            Mana.Base = worldState.CreaturesDatabase[ID].Mana;
            Mana.Current = Mana.Maximum;
            Life.Base = worldState.CreaturesDatabase[ID].Life;
            Life.Current = Life.Maximum;
            Faction = worldState.CreaturesDatabase[ID].Faction;
            byte i = 0;
            checked
            {
                do
                {
                    Resistances[i].Base = worldState.CreaturesDatabase[ID].Resistances[i];
                    i = (byte)unchecked((uint)(i + 1));
                }
                while (i <= 6u);
                if (EquipmentID == 0 && worldState.CreaturesDatabase[ID].EquipmentID > 0)
                {
                    EquipmentID = worldState.CreaturesDatabase[ID].EquipmentID;
                }
                if (database.CreatureModel.ContainsKey(Model))
                {
                    BoundingRadius = database.CreatureModel[Model].BoundingRadius;
                    CombatReach = database.CreatureModel[Model].CombatReach;
                }
                MechanicImmunity = worldState.CreaturesDatabase[ID].MechanicImmune;
                CanSeeInvisibility_Stealth = 5 * Level;
                CanSeeInvisibility_Invisibility = 0;
                if ((worldState.CreaturesDatabase[ID].cNpcFlags & 0x20) == 32)
                {
                    Invisibility = InvisibilityLevel.DEAD;
                    cUnitFlags = 16777318;
                }
                cDynamicFlags = worldState.CreaturesDatabase[ID].DynFlags;
                StandState = cStandState;
                cBytes2 = 1;
                if (this is WS_Pets.PetObject)
                {
                    var Creature = this;
                    aiScript = petAiFactory.Create(ref Creature);
                    return;
                }
                if (Operators.CompareString(worldState.CreaturesDatabase[ID].AIScriptSource, "", TextCompare: false) != 0)
                {
                    aiScript = (WS_Creatures_AI.TBaseAI)scriptExecutor.InvokeConstructor(worldState.CreaturesDatabase[ID].AIScriptSource, new object[1]
                    {
                            this
                    });
                }
                else if (File.Exists("scripts\\creatures\\" + Globals.Functions.FixName(Name) + ".vb"))
                {
                    ScriptedObject tmpScript = new("scripts\\creatures\\" + Globals.Functions.FixName(Name) + ".vb", "", InMemory: true);
                    aiScript = (WS_Creatures_AI.TBaseAI)tmpScript.InvokeConstructor("CreatureAI_" + Globals.Functions.FixName(Name).Replace(" ", "_"), new object[1]
                    {
                            this
                    });
                    tmpScript.Dispose();
                }
                if (aiScript != null)
                {
                    return;
                }
                if (IsCritter)
                {
                    var Creature = this;
                    aiScript = critterAiFactory.Create(ref Creature);
                }
                else if (IsGuard)
                {
                    if (MoveType == 2)
                    {
                        var Creature = this;
                        aiScript = guardWaypointAiFactory.Create(ref Creature);
                    }
                    else
                    {
                        var Creature = this;
                        aiScript = guardAiFactory.Create(ref Creature);
                    }
                }
                else if (MoveType == 1)
                {
                    var Creature = this;
                    aiScript = defaultAiFactory.Create(ref Creature);
                }
                else if (MoveType == 2)
                {
                    var Creature = this;
                    aiScript = waypointAiFactory.Create(ref Creature);
                }
                else
                {
                    var Creature = this;
                    aiScript = standStillAiFactory.Create(ref Creature);
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                aiScript?.Dispose();
                RemoveFromWorld();
                try
                {
                    worldState.WorldCreaturesLock.EnterWriteLock();
                    worldState.WorldCreatures.Remove(GUID);
                    worldState.WorldCreatureKeys.Remove(GUID);
                    worldState.WorldCreaturesLock.ExitWriteLock();
                    ExpireTimer?.Dispose();
                }
                catch (Exception ex2)
                {
                    ProjectData.SetProjectError(ex2);
                    var ex = ex2;
                    baseLogger.LogWarning("WS_Creatures:Dispose failed -  {0}", ex.Message);
                    ProjectData.ClearProjectError();
                }
            }
            _disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        void IDisposable.Dispose()
        {
            //ILSpy generated this explicit interface implementation from .override directive in Dispose
            Dispose();
        }

        public void Destroy(object state = null)
        {
            if (decimal.Compare(new decimal(SummonedBy), 0m) > 0 && LegacyGlobalFunctions.GuidIsPlayer(SummonedBy) && worldState.Characters.ContainsKey(SummonedBy) && worldState.Characters[SummonedBy].NonCombatPet != null && worldState.Characters[SummonedBy].NonCombatPet == this)
            {
                worldState.Characters[SummonedBy].NonCombatPet = null;
            }
            Packets.PacketClass packet = new(Opcodes.SMSG_DESTROY_OBJECT);
            packet.AddUInt64(GUID);
            SendToNearPlayers(ref packet);
            packet.Dispose();
            Dispose();
        }

        public void Despawn()
        {
            RemoveFromWorld();
            if (WS_Loot.LootTable.ContainsKey(GUID))
            {
                WS_Loot.LootTable[GUID].Dispose();
            }
            if (SpawnTime > 0)
            {
                if (aiScript != null)
                {
                    aiScript.State = AIState.AI_RESPAWN;
                    aiScript.Pause(checked(SpawnTime * 1000));
                }
            }
            else
            {
                Dispose();
            }
        }

        public void Respawn()
        {
            Life.Current = Life.Maximum;
            Mana.Current = Mana.Maximum;
            cUnitFlags &= -16385;
            cDynamicFlags = 0;
            positionX = SpawnX;
            positionY = SpawnY;
            positionZ = SpawnZ;
            orientation = SpawnO;
            if (aiScript != null)
            {
                aiScript.OnLeaveCombat(Reset: false);
                aiScript.State = AIState.AI_WANDERING;
            }
            if (SeenBy.Count > 0)
            {
                Packets.UpdatePacketClass packetForNear = new();
                var UpdateData = updateClassFactory.Create(188);
                UpdateData.SetUpdateFlag(22, Life.Current);
                UpdateData.SetUpdateFlag((int)checked(23 + base.ManaType), Mana.Current);
                UpdateData.SetUpdateFlag(46, cUnitFlags);
                UpdateData.SetUpdateFlag(143, cDynamicFlags);
                Packets.PacketClass packet = packetForNear;
                var updateObject = this;
                UpdateData.AddToPacket(ref packet, ObjectUpdateType.UPDATETYPE_VALUES, ref updateObject);
                packet = packetForNear;
                SendToNearPlayers(ref packet);
                packetForNear = (Packets.UpdatePacketClass)packet;
                packetForNear.Dispose();
                UpdateData.Dispose();
                MoveToInstant(SpawnX, SpawnY, SpawnZ, SpawnO);
            }
            else
            {
                AddToWorld();
            }
        }

        public void AddToWorld()
        {
            maps.GetMapTile(positionX, positionY, ref CellX, ref CellY);
            if (maps.Maps[MapID].Tiles[CellX, CellY] == null)
            {
                tileLoader.LoadMap(CellX, CellY, MapID);
            }
            try
            {
                maps.Maps[MapID].Tiles[CellX, CellY].CreaturesHere.Add(GUID);
            }
            catch (Exception ex2)
            {
                ProjectData.SetProjectError(ex2);
                var ex = ex2;
                baseLogger.LogWarning("WS_Creatures:AddToWorld failed - Guid: {1} ID: {2}  {0}", ex.Message, GUID, ID);
                ProjectData.ClearProjectError();
                return;
            }
            short i = -1;
            checked
            {
                do
                {
                    short j = -1;
                    do
                    {
                        if ((short)unchecked(CellX + i) >= 0 && (short)unchecked(CellX + i) <= 63 && (short)unchecked(CellY + j) >= 0 && (short)unchecked(CellY + j) <= 63 && maps.Maps[MapID].Tiles[(short)unchecked(CellX + i), (short)unchecked(CellY + j)] != null && maps.Maps[MapID].Tiles[(short)unchecked(CellX + i), (short)unchecked(CellY + j)].PlayersHere.Count > 0)
                        {
                            var tMapTile = maps.Maps[MapID].Tiles[(short)unchecked(CellX + i), (short)unchecked(CellY + j)];
                            var list = tMapTile.PlayersHere.ToArray();
                            var array = list;
                            foreach (var plGUID in array)
                            {
                                var characterObject = worldState.Characters[plGUID];
                                WS_Base.BaseObject objCharacter = this;
                                if (characterObject.CanSee(ref objCharacter))
                                {
                                    Packets.PacketClass packet = new(Opcodes.SMSG_UPDATE_OBJECT);
                                    try
                                    {
                                        packet.AddInt32(1);
                                        packet.AddInt8(0);
                                        var tmpUpdate = updateClassFactory.Create(MangosGlobalConstants.FIELD_MASK_SIZE_UNIT);
                                        FillAllUpdateFlags(ref tmpUpdate);
                                        var updateClass = tmpUpdate;
                                        var updateObject = this;
                                        updateClass.AddToPacket(ref packet, ObjectUpdateType.UPDATETYPE_CREATE_OBJECT, ref updateObject);
                                        tmpUpdate.Dispose();
                                        worldState.Characters[plGUID].client.SendMultiplyPackets(ref packet);
                                        worldState.Characters[plGUID].creaturesNear.Add(GUID);
                                        SeenBy.Add(plGUID);
                                    }
                                    finally
                                    {
                                        packet.Dispose();
                                    }
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

        public void RemoveFromWorld()
        {
            try
            {
                maps.GetMapTile(positionX, positionY, ref CellX, ref CellY);
                maps.Maps[MapID].Tiles[CellX, CellY].CreaturesHere.Remove(GUID);
                var array = SeenBy.ToArray();
                foreach (var plGUID in array)
                {
                    if (worldState.Characters.ContainsKey(plGUID) && plGUID != 0)
                    {
                        worldState.Characters[plGUID].guidsForRemoving_Lock.AcquireWriterLock(MangosGlobalConstants.DEFAULT_LOCK_TIMEOUT);
                        worldState.Characters[plGUID].guidsForRemoving.Add(GUID);
                        worldState.Characters[plGUID].guidsForRemoving_Lock.ReleaseWriterLock();
                        worldState.Characters[plGUID].creaturesNear.Remove(GUID);
                    }
                }
                SeenBy.Clear();
            }
            catch (Exception ex)
            {
                baseLogger.LogWarning("WS_Creatures:RemoveFromWorld - Unable to remove creatue from world, Remove again  {0}", ex.Message);
                maps.Maps[MapID].Tiles[CellX, CellY].CreaturesHere.Remove(GUID);
            }
        }

        public void MoveCell()
        {
            try
            {
                if (CellX != maps.GetMapTileX(positionX) || CellY != maps.GetMapTileY(positionY))
                {
                    if (maps.Maps[MapID].Tiles != null && !Information.IsNothing(maps.Maps[MapID].Tiles[CellX, CellY].CreaturesHere.Remove(GUID)))
                    {
                        maps.Maps[MapID].Tiles[CellX, CellY].CreaturesHere.Remove(GUID);
                    }
                    maps.GetMapTile(positionX, positionY, ref CellX, ref CellY);
                    if (maps.Maps[MapID].Tiles[CellX, CellY] == null)
                    {
                        aiScript.Reset();
                    }
                    else
                    {
                        maps.Maps[MapID].Tiles[CellX, CellY].CreaturesHere.Add(GUID);
                    }
                }
            }
            catch (Exception ex2)
            {
                ProjectData.SetProjectError(ex2);
                var e = ex2;
                baseLogger.LogWarning("WS_Creatures:MoveCell - Creature outside of map bounds, Resetting  {0}", e.Message);
                try
                {
                    aiScript.Reset();
                }
                catch (Exception ex3)
                {
                    ProjectData.SetProjectError(ex3);
                    var ex = ex3;
                    baseLogger.LogError("WS_Creatures:MoveCell - Couldn't reset creature outside of map bounds, Disposing  {0}", ex.Message);
                    aiScript.Dispose();
                    ProjectData.ClearProjectError();
                }
                ProjectData.ClearProjectError();
            }
        }
    }

    public class NPCText
    {
        public byte Count;
        private readonly WS_Creatures creatures;
        private readonly WorldDatabase worldDatabase;
        public int TextID;

        public float[] Probability;

        public int[] Language;

        public string[] TextLine1;

        public string[] TextLine2;

        public int[] Emote1;

        public int[] Emote2;

        public int[] Emote3;

        public int[] EmoteDelay1;

        public int[] EmoteDelay2;

        public int[] EmoteDelay3;

        public NPCText(WS_Creatures creatures, WorldDatabase worldDatabase, int _TextID)
        {
            Count = 1;
            TextID = 0;
            Probability = new float[8];
            Language = new int[8];
            TextLine1 = new string[8]
            {
                    "",
                    "",
                    "",
                    "",
                    "",
                    "",
                    "",
                    ""
            };
            TextLine2 = new string[8]
            {
                    "",
                    "",
                    "",
                    "",
                    "",
                    "",
                    "",
                    ""
            };
            Emote1 = new int[8];
            Emote2 = new int[8];
            Emote3 = new int[8];
            EmoteDelay1 = new int[8];
            EmoteDelay2 = new int[8];
            EmoteDelay3 = new int[8];
            this.creatures = creatures;
            this.worldDatabase = worldDatabase;
            TextID = _TextID;
            DataTable MySQLQuery = new();
            worldDatabase.Query($"SELECT * FROM npc_text WHERE ID = {TextID};", ref MySQLQuery);
            checked
            {
                if (MySQLQuery.Rows.Count > 0)
                {
                    var i = 0;
                    do
                    {
                        Probability[i] = MySQLQuery.Rows[0].As<float>(("prob" + Conversions.ToString(i)) ?? "");
                        if (!Information.IsDBNull(RuntimeHelpers.GetObjectValue(MySQLQuery.Rows[0]["text" + Conversions.ToString(i) + "_0"])))
                        {
                            TextLine1[i] = MySQLQuery.Rows[0].As<string>("text" + Conversions.ToString(i) + "_0");
                        }
                        if (!Information.IsDBNull(RuntimeHelpers.GetObjectValue(MySQLQuery.Rows[0]["text" + Conversions.ToString(i) + "_1"])))
                        {
                            TextLine2[i] = MySQLQuery.Rows[0].As<string>("text" + Conversions.ToString(i) + "_1");
                        }
                        if (!Information.IsDBNull(RuntimeHelpers.GetObjectValue(MySQLQuery.Rows[0][("lang" + Conversions.ToString(i)) ?? ""])))
                        {
                            Language[i] = MySQLQuery.Rows[0].As<int>(("lang" + Conversions.ToString(i)) ?? "");
                        }
                        if (!Information.IsDBNull(RuntimeHelpers.GetObjectValue(MySQLQuery.Rows[0]["em" + Conversions.ToString(i) + "_0_delay"])))
                        {
                            EmoteDelay1[i] = MySQLQuery.Rows[0].As<int>("em" + Conversions.ToString(i) + "_0_delay");
                        }
                        if (!Information.IsDBNull(RuntimeHelpers.GetObjectValue(MySQLQuery.Rows[0]["em" + Conversions.ToString(i) + "_0"])))
                        {
                            Emote1[i] = MySQLQuery.Rows[0].As<int>("em" + Conversions.ToString(i) + "_0");
                        }
                        if (!Information.IsDBNull(RuntimeHelpers.GetObjectValue(MySQLQuery.Rows[0]["em" + Conversions.ToString(i) + "_1_delay"])))
                        {
                            EmoteDelay2[i] = MySQLQuery.Rows[0].As<int>("em" + Conversions.ToString(i) + "_1_delay");
                        }
                        if (!Information.IsDBNull(RuntimeHelpers.GetObjectValue(MySQLQuery.Rows[0]["em" + Conversions.ToString(i) + "_1"])))
                        {
                            Emote2[i] = MySQLQuery.Rows[0].As<int>("em" + Conversions.ToString(i) + "_1");
                        }
                        if (!Information.IsDBNull(RuntimeHelpers.GetObjectValue(MySQLQuery.Rows[0]["em" + Conversions.ToString(i) + "_2_delay"])))
                        {
                            EmoteDelay3[i] = MySQLQuery.Rows[0].As<int>("em" + Conversions.ToString(i) + "_2_delay");
                        }
                        if (!Information.IsDBNull(RuntimeHelpers.GetObjectValue(MySQLQuery.Rows[0]["em" + Conversions.ToString(i) + "_2"])))
                        {
                            Emote3[i] = MySQLQuery.Rows[0].As<int>("em" + Conversions.ToString(i) + "_2");
                        }
                        if (Operators.CompareString(TextLine1[i], "", TextCompare: false) != 0)
                        {
                            Count = (byte)(checked((byte)i) + 1);
                        }
                        i++;
                    }
                    while (i <= 7);
                }
                else
                {
                    Probability[0] = 1f;
                    TextLine1[0] = "Hey there, $N. How can I help you?";
                    TextLine2[0] = TextLine1[0];
                    Count = 0;
                }
                WS_Creatures.NPCTexts.Add(TextID, this);
            }
        }

        public NPCText(WS_Creatures creatures, WorldDatabase worldDatabase, int _TextID, string TextLine)
        {
            Count = 1;
            TextID = 0;
            Probability = new float[8];
            Language = new int[8];
            TextLine1 = new string[8]
            {
                    "",
                    "",
                    "",
                    "",
                    "",
                    "",
                    "",
                    ""
            };
            TextLine2 = new string[8]
            {
                    "",
                    "",
                    "",
                    "",
                    "",
                    "",
                    "",
                    ""
            };
            Emote1 = new int[8];
            Emote2 = new int[8];
            Emote3 = new int[8];
            EmoteDelay1 = new int[8];
            EmoteDelay2 = new int[8];
            EmoteDelay3 = new int[8];
            this.creatures = creatures;
            this.worldDatabase = worldDatabase;
            TextID = _TextID;
            TextLine1[0] = TextLine;
            TextLine2[0] = TextLine;
            Count = 0;
            WS_Creatures.NPCTexts.Add(TextID, this);
        }
    }

    public const int SKILL_DETECTION_PER_LEVEL = 5;
    private readonly ILogger<WS_Creatures> logger;
    private readonly WorldState worldState;
    private readonly WorldDatabase worldDatabase;
    private readonly WS_Handlers_Misc misc;
    private readonly ICharacterResurrectionService characterResurrectionService;
    private readonly MangosConfiguration configuration;
    public int[] CorpseDecay;

    public static Dictionary<int, NPCText> NPCTexts { get; set; }

    public WS_Creatures(
        ILogger<WS_Creatures> logger,
        WorldState worldState,
        WorldDatabase worldDatabase,
        WS_Network network,
        WS_Handlers_Misc misc,
        ICharacterResurrectionService characterResurrectionService)
    {
        CorpseDecay = new int[5]
        {
                30,
                150,
                150,
                150,
                1800
        };
        NPCTexts = new Dictionary<int, NPCText>();
        this.logger = logger;
        this.worldState = worldState;
        this.worldDatabase = worldDatabase;
        this.misc = misc;
        this.characterResurrectionService = characterResurrectionService;
    }

    public void On_CMSG_CREATURE_QUERY(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        checked
        {
            if (packet.Data.Length - 1 < 17)
            {
                return;
            }
            Packets.PacketClass response = new(Opcodes.SMSG_CREATURE_QUERY_RESPONSE);
            packet.GetInt16();
            var CreatureID = packet.GetInt32();
            var CreatureGUID = packet.GetUInt64();
            try
            {
                if (!worldState.CreaturesDatabase.ContainsKey(CreatureID) && CreatureID != 0 && CreatureGUID != 0)
                {
                    logger.LogDebug("[{0}:{1}] CMSG_CREATURE_QUERY [Creature {2} not loaded.]", client.IP, client.Port, CreatureID);
                    response.AddUInt32((uint)(CreatureID | int.MinValue));
                    client.Send(ref response);
                    response.Dispose();
                    return;
                }
                var Creature = worldState.CreaturesDatabase[CreatureID];
                response.AddInt32(Creature.Id);
                response.AddString(Creature.Name);
                response.AddInt8(0);
                response.AddInt8(0);
                response.AddInt8(0);
                response.AddString(Creature.SubName);
                response.AddInt32((int)Creature.TypeFlags);
                response.AddInt32(Creature.CreatureType);
                response.AddInt32(Creature.CreatureFamily);
                response.AddInt32(Creature.Elite);
                response.AddInt32(0);
                response.AddInt32(Creature.PetSpellDataID);
                response.AddInt32(Creature.ModelA1);
                response.AddInt32(Creature.ModelA2);
                response.AddInt32(Creature.ModelH1);
                response.AddInt32(Creature.ModelH2);
                response.AddSingle(1f);
                response.AddSingle(1f);
                response.AddInt8(Creature.Leader);
                client.Send(ref response);
                response.Dispose();
            }
            catch (Exception ex2)
            {
                ProjectData.SetProjectError(ex2);
                var ex = ex2;
                logger.LogError("Unknown Error: Unable to find CreatureID={0} in database. {1}", CreatureID, ex.Message);
                ProjectData.ClearProjectError();
            }
        }
    }

    public void On_CMSG_NPC_TEXT_QUERY(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        checked
        {
            if (packet.Data.Length - 1 >= 17)
            {
                packet.GetInt16();
                long TextID = packet.GetInt32();
                var TargetGUID = packet.GetUInt64();
                logger.LogDebug("[{0}:{1}] CMSG_NPC_TEXT_QUERY [TextID={2}]", client.IP, client.Port, TextID);
                client.Character.SendTalking((int)TextID);
            }
        }
    }

    public void On_CMSG_GOSSIP_HELLO(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        if (checked(packet.Data.Length - 1) < 13)
        {
            return;
        }
        packet.GetInt16();
        var GUID = packet.GetUInt64();
        logger.LogDebug("[{0}:{1}] CMSG_GOSSIP_HELLO [GUID={2:X}]", client.IP, client.Port, GUID);
        if (!worldState.WorldCreatures.ContainsKey(GUID) || worldState.WorldCreatures[GUID].CreatureInfo.cNpcFlags == 0)
        {
            logger.LogWarning("[{0}:{1}] Client tried to speak with a creature that didn't exist or couldn't interact with. [GUID={2:X}  ID={3}]", client.IP, client.Port, GUID, worldState.WorldCreatures[GUID].ID);
        }
        else
        {
            if (worldState.WorldCreatures[GUID].Evade)
            {
                return;
            }
            worldState.WorldCreatures[GUID].StopMoving();
            client.Character.RemoveAurasByInterruptFlag(1024);
            try
            {
                if (worldState.CreaturesDatabase[worldState.WorldCreatures[GUID].ID].TalkScript == null)
                {
                    Packets.PacketClass test = new(Opcodes.SMSG_NPC_WONT_TALK);
                    test.AddUInt64(GUID);
                    test.AddInt8(1);
                    client.Send(ref test);
                    test.Dispose();
                    if (!NPCTexts.ContainsKey(34))
                    {
                        NPCText tmpText = new(this, worldDatabase, 34, "Hi $N, I'm not yet scripted to talk with you.");
                    }
                    client.Character.SendTalking(34);
                    var character = client.Character;
                    GossipMenu Menu = null;
                    QuestMenu qMenu = null;
                    character.SendGossip(GUID, 34, Menu, qMenu);
                }
                else
                {
                    worldState.CreaturesDatabase[worldState.WorldCreatures[GUID].ID].TalkScript.OnGossipHello(ref client.Character, GUID);
                }
            }
            catch (Exception ex2)
            {
                ProjectData.SetProjectError(ex2);
                var ex = ex2;
                logger.LogCritical("Error in gossip hello.{0}{1}", Environment.NewLine, ex.ToString());
                ProjectData.ClearProjectError();
            }
        }
    }

    public void On_CMSG_GOSSIP_SELECT_OPTION(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        if (checked(packet.Data.Length - 1) < 17)
        {
            return;
        }
        packet.GetInt16();
        var GUID = packet.GetUInt64();
        var SelOption = packet.GetInt32();
        logger.LogDebug("[{0}:{1}] CMSG_GOSSIP_SELECT_OPTION [SelOption={3} GUID={2:X}]", client.IP, client.Port, GUID, SelOption);
        if (!worldState.WorldCreatures.ContainsKey(GUID) || worldState.WorldCreatures[GUID].CreatureInfo.cNpcFlags == 0)
        {
            logger.LogWarning("[{0}:{1}] Client tried to speak with a creature that didn't exist or couldn't interact with. [GUID={2:X}  ID={3}]", client.IP, client.Port, GUID, worldState.WorldCreatures[GUID].ID);
        }
        else
        {
            if (worldState.CreaturesDatabase[worldState.WorldCreatures[GUID].ID].TalkScript == null)
            {
                throw new ApplicationException("Invoked OnGossipSelect() on creature without initialized TalkScript!");
            }
            worldState.CreaturesDatabase[worldState.WorldCreatures[GUID].ID].TalkScript.OnGossipSelect(ref client.Character, GUID, SelOption);
        }
    }

    public void On_CMSG_SPIRIT_HEALER_ACTIVATE(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        checked
        {
            if (packet.Data.Length - 1 < 13)
            {
                return;
            }
            packet.GetInt16();
            var GUID = packet.GetUInt64();
            logger.LogDebug("[{0}:{1}] CMSG_SPIRIT_HEALER_ACTIVATE [GUID={2}]", client.IP, client.Port, GUID);
            try
            {
                byte i = 0;
                do
                {
                    if (client.Character.Items.ContainsKey(i))
                    {
                        client.Character.Items[i].ModifyDurability(0.25f, ref client);
                    }
                    i = (byte)unchecked((uint)(i + 1));
                }
                while (i <= 18u);
            }
            catch (Exception ex2)
            {
                ProjectData.SetProjectError(ex2);
                var ex = ex2;
                logger.LogError("Error activating spirit healer: {0}", ex.ToString());
                ProjectData.ClearProjectError();
            }

            characterResurrectionService.CharacterResurrect(ref client.Character);
            client.Character.ApplySpell(15007);
        }
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    private ulong GetNewGUID()
    {
        return ++WorldState.CreatureGuidCounter;
    }
}
