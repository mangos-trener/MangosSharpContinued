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
using Mangos.Common.Legacy.Databases;
using Mangos.Common.Legacy.Globals;
using Mangos.World.Globals;
using Mangos.World.Handlers;
using Mangos.World.Loots;
using Mangos.World.Maps;
using Mangos.World.Objects.Factories;
using Mangos.World.Objects.Factories.Loot;
using Mangos.World.Objects.Factories.Packets;
using Mangos.World.Player;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;

namespace Mangos.World.Objects;
public class GameObject : WS_Base.BaseObject, IDisposable
{
    private readonly WS_Maps maps;
    private readonly WS_Combat combat;
    private readonly WS_Loot loot;
    private readonly LootObjectFactory lootObjectFactory;
    private readonly UpdateClassFactory updateClassFactory;
    private readonly ILogger<GameObject> logger;
    public int ID;

    public int Flags;

    public float Size;

    public int Faction;

    public GameObjectLootState State;

    public float[] Rotations;

    public ulong Owner;

    public WS_Loot.LootObject Loot;

    public bool Despawned;

    public int MineRemaining;

    public int AnimProgress;

    public int SpawnTime;

    public int GameEvent;

    public int CreatedBySpell;

    public int Level;

    public bool ToDespawn;

    public List<int> IncludesQuestItems;

    public Timer RespawnTimer;

    private bool _disposedValue;

    public GameObjectInfo ObjectInfo => worldState.GameObjectsDatabase[ID];

    public string Name => ObjectInfo.Name;

    public GameObjectType Type => ObjectInfo.Type;

    public uint GetSound(int Index)
    {
        return ObjectInfo.Fields[Index];
    }

    public bool IsUsedForQuests => IncludesQuestItems.Count > 0;

    public int LockID => checked(ObjectInfo.Type switch
    {
        GameObjectType.GAMEOBJECT_TYPE_DOOR => (int)GetSound(1),
        GameObjectType.GAMEOBJECT_TYPE_BUTTON => (int)GetSound(1),
        GameObjectType.GAMEOBJECT_TYPE_QUESTGIVER => (int)GetSound(0),
        GameObjectType.GAMEOBJECT_TYPE_CHEST => (int)GetSound(0),
        GameObjectType.GAMEOBJECT_TYPE_TRAP => (int)GetSound(0),
        GameObjectType.GAMEOBJECT_TYPE_GOOBER => (int)GetSound(0),
        GameObjectType.GAMEOBJECT_TYPE_AREADAMAGE => (int)GetSound(0),
        GameObjectType.GAMEOBJECT_TYPE_CAMERA => (int)GetSound(0),
        GameObjectType.GAMEOBJECT_TYPE_FLAGSTAND => (int)GetSound(0),
        GameObjectType.GAMEOBJECT_TYPE_FISHINGHOLE => (int)GetSound(4),
        GameObjectType.GAMEOBJECT_TYPE_FLAGDROP => (int)GetSound(0),
        _ => 0,
    });

    public int LootID => checked(ObjectInfo.Type switch
    {
        GameObjectType.GAMEOBJECT_TYPE_CHEST => (int)GetSound(1),
        GameObjectType.GAMEOBJECT_TYPE_FISHINGNODE => (int)GetSound(1),
        GameObjectType.GAMEOBJECT_TYPE_FISHINGHOLE => (int)GetSound(1),
        _ => 0,
    });

    public int AutoCloseTime => checked(ObjectInfo.Type switch
    {
        GameObjectType.GAMEOBJECT_TYPE_DOOR => (int)Math.Round(GetSound(2) / 65536.0 * 1000.0),
        GameObjectType.GAMEOBJECT_TYPE_BUTTON => (int)Math.Round(GetSound(2) / 65536.0 * 1000.0),
        GameObjectType.GAMEOBJECT_TYPE_TRAP => (int)Math.Round(GetSound(6) / 65536.0 * 1000.0),
        GameObjectType.GAMEOBJECT_TYPE_GOOBER => (int)Math.Round(GetSound(3) / 65536.0 * 1000.0),
        GameObjectType.GAMEOBJECT_TYPE_TRANSPORT => (int)Math.Round(GetSound(2) / 65536.0 * 1000.0),
        GameObjectType.GAMEOBJECT_TYPE_AREADAMAGE => (int)Math.Round(GetSound(5) / 65536.0 * 1000.0),
        _ => 0,
    });

    public bool IsConsumeable
    {
        get
        {
            var type = ObjectInfo.Type;
            return type == GameObjectType.GAMEOBJECT_TYPE_CHEST && (ulong)GetSound(3) == 1;
        }
    }

    public virtual void FillAllUpdateFlags(ref Packets.UpdateClass Update, ref CharacterObject Character)
    {
        Update.SetUpdateFlag(0, GUID);
        Update.SetUpdateFlag(2, 33);
        Update.SetUpdateFlag(3, ID);
        Update.SetUpdateFlag(4, Size);
        if (Owner != 0)
        {
            Update.SetUpdateFlag(6, Owner);
        }
        Update.SetUpdateFlag(15, positionX);
        Update.SetUpdateFlag(16, positionY);
        Update.SetUpdateFlag(17, positionZ);
        Update.SetUpdateFlag(18, orientation);
        var Rotation = 0L;
        var f_rot1 = (float)Math.Sin(orientation / 2f);
        var i_rot1 = checked((long)Math.Round(f_rot1 / Math.Atan(Math.Pow(2.0, -20.0))));
        Rotation |= i_rot1 << 43 >> 43 & 0x1FFFFF;
        Update.SetUpdateFlag(10, Rotation);
        var DynFlags = 0;
        if (Type == GameObjectType.GAMEOBJECT_TYPE_CHEST)
        {
            var aLLQUESTS = worldState.QuestsService;
            var gameobject = this;
            var UsedForQuest = aLLQUESTS.IsGameObjectUsedForQuest(ref gameobject, ref Character);
            if (UsedForQuest > 0)
            {
                Flags |= 4;
                if (UsedForQuest == 2)
                {
                    DynFlags = 9;
                }
            }
        }
        else if (Type == GameObjectType.GAMEOBJECT_TYPE_GOOBER)
        {
            DynFlags = 1;
        }
        if (DynFlags != 0)
        {
            Update.SetUpdateFlag(19, DynFlags);
        }
        Update.SetUpdateFlag(14, 0, (byte)State);
        Update.SetUpdateFlag(21, (int)Type);
        if (Level > 0)
        {
            Update.SetUpdateFlag(22, Level);
        }
        Update.SetUpdateFlag(20, Faction);
        Update.SetUpdateFlag(9, Flags);
        Update.SetUpdateFlag(8, ObjectInfo.Model);
        Update.SetUpdateFlag(10, Rotations[0]);
        Update.SetUpdateFlag(11, Rotations[1]);
        Update.SetUpdateFlag(12, Rotations[2]);
        Update.SetUpdateFlag(13, Rotations[3]);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            RemoveFromWorld();
            if (Loot != null && Type != GameObjectType.GAMEOBJECT_TYPE_FISHINGNODE)
            {
                Loot.Dispose();
            }
            if (this is WS_Transports.TransportObject)
            {
                worldState.WorldTransportsLock.EnterWriteLock();
                worldState.WorldTransports.Remove(GUID);
                worldState.WorldTransportsLock.ExitReadLock();
                RespawnTimer.Dispose();
            }
            else
            {
                worldState.WorldGameObjects.Remove(GUID);
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

    public GameObject(ILogger<GameObject> logger, WorldState worldState, WS_Maps maps, WS_Loot loot, WS_Combat combat, LootObjectFactory lootObjectFactory, UpdateClassFactory updateClassFactory)
        : base(worldState, null)
    {
        this.logger = logger;
        this.maps = maps;
        this.loot = loot;
        this.combat = combat;
        this.lootObjectFactory = lootObjectFactory;
        this.updateClassFactory = updateClassFactory;
    }

    public GameObject(
        ILogger<GameObject> logger,
        WorldState worldState,
        WorldDatabase worldDatabase,
        WS_Maps maps,
        WS_Loot loot,
        WS_Combat combat,
        GameObjectInfoFactory gameObjectInfoFactory,
        LootObjectFactory lootObjectFactory,
        UpdateClassFactory updateClassFactory,
        int ID_)
        : this(logger, worldState, maps, loot, combat, lootObjectFactory, updateClassFactory)
    {
        ID = 0;
        Flags = 0;
        Size = 1f;
        Faction = 0;
        State = GameObjectLootState.DOOR_CLOSED;
        Rotations = new float[4];
        Loot = null;
        Despawned = false;
        MineRemaining = 0;
        AnimProgress = 0;
        SpawnTime = 0;
        GameEvent = 0;
        CreatedBySpell = 0;
        Level = 0;
        ToDespawn = false;
        IncludesQuestItems = new List<int>();
        RespawnTimer = null;
        if (!worldState.GameObjectsDatabase.ContainsKey(ID_))
        {
            var baseGameObject = gameObjectInfoFactory.Create(ID_);
        }

        this.maps = maps;
        this.lootObjectFactory = lootObjectFactory;
        ID = ID_;
        GUID = WS_GameObjects.GetNewGUID();
        Flags = worldState.GameObjectsDatabase[ID].Flags;
        Faction = worldState.GameObjectsDatabase[ID].Faction;
        Size = worldState.GameObjectsDatabase[ID].Size;
        worldState.WorldGameObjects.Add(GUID, this);
    }

    public GameObject(
        ILogger<GameObject> logger,
        WorldState worldState,
        WorldDatabase worldDatabase,
        WS_Maps maps,
        WS_Loot loot,
        WS_Combat combat,
        GameObjectInfoFactory gameObjectInfoFactory,
        LootObjectFactory lootObjectFactory,
        UpdateClassFactory updateClassFactory,
        int ID_,
        ulong GUID_)
        : this(logger, worldState, maps, loot, combat, lootObjectFactory, updateClassFactory)
    {
        ID = 0;
        Flags = 0;
        Size = 1f;
        Faction = 0;
        State = GameObjectLootState.DOOR_CLOSED;
        Rotations = new float[4];
        Loot = null;
        Despawned = false;
        MineRemaining = 0;
        AnimProgress = 0;
        SpawnTime = 0;
        GameEvent = 0;
        CreatedBySpell = 0;
        Level = 0;
        ToDespawn = false;
        IncludesQuestItems = new List<int>();
        RespawnTimer = null;
        if (!worldState.GameObjectsDatabase.ContainsKey(ID_))
        {
            var baseGameObject = gameObjectInfoFactory.Create(ID_);
        }

        this.maps = maps;
        this.lootObjectFactory = lootObjectFactory;
        ID = ID_;
        GUID = GUID_;
        Flags = worldState.GameObjectsDatabase[ID].Flags;
        Faction = worldState.GameObjectsDatabase[ID].Faction;
        Size = worldState.GameObjectsDatabase[ID].Size;
    }

    public GameObject(
        ILogger<GameObject> logger,
        WorldState worldState,
        WorldDatabase worldDatabase,
        WS_Maps maps,
        WS_Loot loot,
        WS_Combat combat,
        GameObjectInfoFactory gameObjectInfoFactory,
        LootObjectFactory lootObjectFactory,
        UpdateClassFactory updateClassFactory,
        int ID_,
        uint MapID_,
        float PosX,
        float PosY,
        float PosZ,
        float Rotation,
        ulong Owner_ = 0uL)
        : this(logger, worldState, maps, loot, combat, lootObjectFactory, updateClassFactory)
    {
        ID = 0;
        Flags = 0;
        Size = 1f;
        Faction = 0;
        State = GameObjectLootState.DOOR_CLOSED;
        Rotations = new float[4];
        Loot = null;
        Despawned = false;
        MineRemaining = 0;
        AnimProgress = 0;
        SpawnTime = 0;
        GameEvent = 0;
        CreatedBySpell = 0;
        Level = 0;
        ToDespawn = false;
        IncludesQuestItems = new List<int>();
        RespawnTimer = null;
        if (!worldState.GameObjectsDatabase.ContainsKey(ID_))
        {
            var baseGameObject = gameObjectInfoFactory.Create(ID_);
        }

        this.maps = maps;
        this.lootObjectFactory = lootObjectFactory;
        ID = ID_;
        GUID = WS_GameObjects.GetNewGUID();
        MapID = MapID_;
        positionX = PosX;
        positionY = PosY;
        positionZ = PosZ;
        orientation = Rotation;
        Owner = Owner_;
        Flags = worldState.GameObjectsDatabase[ID].Flags;
        Faction = worldState.GameObjectsDatabase[ID].Faction;
        Size = worldState.GameObjectsDatabase[ID].Size;
        if (Type == GameObjectType.GAMEOBJECT_TYPE_TRANSPORT)
        {
            VisibleDistance = 99999f;
            State = GameObjectLootState.DOOR_CLOSED;
        }
        worldState.WorldGameObjects.Add(GUID, this);
    }

    public GameObject(
        ILogger<GameObject> logger,
        WorldState worldState,
        WorldDatabase worldDatabase,
        WS_Maps maps,
        WS_Loot loot,
        WS_Combat combat,
        GameObjectInfoFactory gameObjectInfoFactory,
        LootObjectFactory lootObjectFactory,
        UpdateClassFactory updateClassFactory,
        ulong cGUID,
        DataRow Info = null)
        : this(logger, worldState, maps, loot, combat, lootObjectFactory, updateClassFactory)
    {
        ID = 0;
        Flags = 0;
        Size = 1f;
        Faction = 0;
        State = GameObjectLootState.DOOR_CLOSED;
        Rotations = new float[4];
        Loot = null;
        Despawned = false;
        MineRemaining = 0;
        AnimProgress = 0;
        SpawnTime = 0;
        GameEvent = 0;
        CreatedBySpell = 0;
        Level = 0;
        ToDespawn = false;
        IncludesQuestItems = new List<int>();
        RespawnTimer = null;
        if (Info == null)
        {
            DataTable MySQLQuery = new();
            worldDatabase.Query($"SELECT * FROM gameobject LEFT OUTER JOIN game_event_gameobject ON gameobject.guid = game_event_gameobject.guid WHERE gameobject.guid = {cGUID};", ref MySQLQuery);
            if (MySQLQuery.Rows.Count <= 0)
            {
                logger.LogError("GameObject Spawn not found in database. [cGUID={0:X}]", cGUID);
                return;
            }
            Info = MySQLQuery.Rows[0];
        }
        positionX = Conversions.ToSingle(Info["position_X"]);
        positionY = Conversions.ToSingle(Info["position_Y"]);
        positionZ = Conversions.ToSingle(Info["position_Z"]);
        orientation = Conversions.ToSingle(Info["orientation"]);
        MapID = Conversions.ToUInteger(Info["map"]);
        Rotations[0] = Conversions.ToSingle(Info["rotation0"]);
        Rotations[1] = Conversions.ToSingle(Info["rotation1"]);
        Rotations[2] = Conversions.ToSingle(Info["rotation2"]);
        Rotations[3] = Conversions.ToSingle(Info["rotation3"]);
        ID = Conversions.ToInteger(Info["id"]);
        AnimProgress = Conversions.ToInteger(Info["animprogress"]);
        SpawnTime = Conversions.ToInteger(Info["spawntimesecs"]);
        State = (GameObjectLootState)Conversions.ToByte(Info["state"]);
        if (!worldState.GameObjectsDatabase.ContainsKey(ID))
        {
            var baseGameObject = gameObjectInfoFactory.Create(ID);
        }
        Flags = worldState.GameObjectsDatabase[ID].Flags;
        Faction = worldState.GameObjectsDatabase[ID].Faction;
        Size = worldState.GameObjectsDatabase[ID].Size;
        checked
        {
            if (Type == GameObjectType.GAMEOBJECT_TYPE_TRANSPORT)
            {
                VisibleDistance = 99999f;
                GUID = cGUID + MangosGlobalConstants.GUID_TRANSPORT;
            }
            else
            {
                GUID = cGUID + MangosGlobalConstants.GUID_GAMEOBJECT;
            }
            worldState.WorldGameObjects.Add(GUID, this);
            if (WS_Loot.LootTable.ContainsKey(GUID))
            {
                Loot = WS_Loot.LootTable[GUID];
            }
            CalculateMineRemaning(Force: true);
        }

        this.maps = maps;
        this.combat = combat;
        this.lootObjectFactory = lootObjectFactory;
    }

    public void RemoveFromWorld()
    {
        if (maps.Maps[MapID].Tiles[CellX, CellY] == null)
        {
            return;
        }
        maps.GetMapTile(positionX, positionY, ref CellX, ref CellY);
        maps.Maps[MapID].Tiles[CellX, CellY].GameObjectsHere.Remove(GUID);
        var array = SeenBy.ToArray();
        foreach (var plGUID in array)
        {
            if (worldState.Characters[plGUID].gameObjectsNear.Contains(GUID))
            {
                worldState.Characters[plGUID].guidsForRemoving_Lock.AcquireWriterLock(MangosGlobalConstants.DEFAULT_LOCK_TIMEOUT);
                worldState.Characters[plGUID].guidsForRemoving.Add(GUID);
                worldState.Characters[plGUID].guidsForRemoving_Lock.ReleaseWriterLock();
                worldState.Characters[plGUID].gameObjectsNear.Remove(GUID);
            }
        }
    }

    public void SetState(GameObjectLootState State)
    {
        Packets.PacketClass packet = new(Opcodes.SMSG_UPDATE_OBJECT);
        packet.AddInt32(1);
        packet.AddInt8(0);
        var tmpUpdate = updateClassFactory.Create(MangosGlobalConstants.FIELD_MASK_SIZE_GAMEOBJECT);
        tmpUpdate.SetUpdateFlag(14, 0, (byte)State);
        var updateObject = this;
        tmpUpdate.AddToPacket(ref packet, ObjectUpdateType.UPDATETYPE_VALUES, ref updateObject);
        tmpUpdate.Dispose();
        SendToNearPlayers(ref packet);
        packet.Dispose();
    }

    public void OpenDoor()
    {
        Flags |= 1;
        State = GameObjectLootState.DOOR_OPEN;
        logger.LogDebug("AutoCloseTime: {0}", AutoCloseTime);
        if (AutoCloseTime > 0)
        {
            ThreadPool.RegisterWaitForSingleObject(new AutoResetEvent(initialState: false), CloseDoor, null, AutoCloseTime, executeOnlyOnce: true);
        }
        Packets.PacketClass packet = new(Opcodes.SMSG_UPDATE_OBJECT);
        packet.AddInt32(1);
        packet.AddInt8(0);
        var tmpUpdate = updateClassFactory.Create(MangosGlobalConstants.FIELD_MASK_SIZE_GAMEOBJECT);
        tmpUpdate.SetUpdateFlag(9, Flags);
        tmpUpdate.SetUpdateFlag(14, 0, (byte)State);
        var updateObject = this;
        tmpUpdate.AddToPacket(ref packet, ObjectUpdateType.UPDATETYPE_VALUES, ref updateObject);
        tmpUpdate.Dispose();
        SendToNearPlayers(ref packet);
        packet.Dispose();
    }

    public void CloseDoor(object state, bool timedOut)
    {
        Flags &= 254;
        state = GameObjectLootState.DOOR_CLOSED;
        Packets.PacketClass packet = new(Opcodes.SMSG_UPDATE_OBJECT);
        packet.AddInt32(1);
        packet.AddInt8(0);
        var tmpUpdate = updateClassFactory.Create(MangosGlobalConstants.FIELD_MASK_SIZE_GAMEOBJECT);
        tmpUpdate.SetUpdateFlag(9, Flags);
        tmpUpdate.SetUpdateFlag(14, 0, Conversions.ToByte(state));
        var updateObject = this;
        tmpUpdate.AddToPacket(ref packet, ObjectUpdateType.UPDATETYPE_VALUES, ref updateObject);
        tmpUpdate.Dispose();
        SendToNearPlayers(ref packet);
        packet.Dispose();
    }

    public void LootObject(ref CharacterObject Character, LootType LootingType)
    {
        State = GameObjectLootState.LOOT_LOOTED;
        switch (Type)
        {
            case GameObjectType.GAMEOBJECT_TYPE_DOOR:
            case GameObjectType.GAMEOBJECT_TYPE_BUTTON:
                OpenDoor();
                return;

            case GameObjectType.GAMEOBJECT_TYPE_QUESTGIVER:
                return;
        }
        if (Loot != null)
        {
            Loot.SendLoot(ref Character.client);
            if (Character.spellCasted[1] != null)
            {
                Character.spellCasted[1].State = SpellCastState.SPELL_STATE_FINISHED;
            }
        }
    }

    public void SetupFishingNode()
    {
        var RandomTime = WorldState.Rnd.Next(3000, 17000);
        ThreadPool.RegisterWaitForSingleObject(new AutoResetEvent(initialState: false), SetFishHooked, null, RandomTime, executeOnlyOnce: true);
        State = GameObjectLootState.DOOR_CLOSED;
    }

    public void SetFishHooked(object state, bool timedOut)
    {
        if (!Operators.ConditionalCompareObjectNotEqual(state, GameObjectLootState.DOOR_CLOSED, TextCompare: false))
        {
            state = GameObjectLootState.DOOR_OPEN;
            Flags = 32;
            Loot = lootObjectFactory.Create(GUID, LootType.LOOTTYPE_FISHING, Owner);
            var AreaFlag = maps.GetAreaFlag(positionX, positionY, checked((int)MapID));
            var AreaID = maps.AreaTable[AreaFlag].ID;
            WS_Loot.LootTemplates_Fishing.GetLoot(AreaID)?.Process(ref Loot, 0);
            Packets.PacketClass packet = new(Opcodes.SMSG_UPDATE_OBJECT);
            packet.AddInt32(1);
            packet.AddInt8(0);
            var tmpUpdate = updateClassFactory.Create(MangosGlobalConstants.FIELD_MASK_SIZE_GAMEOBJECT);
            tmpUpdate.SetUpdateFlag(9, Flags);
            tmpUpdate.SetUpdateFlag(14, 0, Conversions.ToByte(state));
            var updateObject = this;
            tmpUpdate.AddToPacket(ref packet, ObjectUpdateType.UPDATETYPE_VALUES, ref updateObject);
            tmpUpdate.Dispose();
            SendToNearPlayers(ref packet);
            packet.Dispose();
            Packets.PacketClass packetAnim = new(Opcodes.SMSG_GAMEOBJECT_CUSTOM_ANIM);
            packetAnim.AddUInt64(GUID);
            packetAnim.AddInt32(0);
            SendToNearPlayers(ref packetAnim);
            packetAnim.Dispose();
            var FishEscapeTime = 2000;
            ThreadPool.RegisterWaitForSingleObject(new AutoResetEvent(initialState: false), SetFishEscaped, null, FishEscapeTime, executeOnlyOnce: true);
        }
    }

    public void SetFishEscaped(object state, bool timedOut)
    {
        if (!Operators.ConditionalCompareObjectNotEqual(state, GameObjectLootState.DOOR_OPEN, TextCompare: false))
        {
            Flags = 2;
            if (Loot != null)
            {
                Loot.Dispose();
                Loot = null;
            }
            if (decimal.Compare(new decimal(Owner), 0m) > 0 && LegacyGlobalFunctions.GuidIsPlayer(Owner) && worldState.Characters.ContainsKey(Owner))
            {
                Packets.PacketClass fishEscaped = new(Opcodes.SMSG_FISH_ESCAPED);
                worldState.Characters[Owner].client.Send(ref fishEscaped);
                fishEscaped.Dispose();
                worldState.Characters[Owner].FinishSpell(CurrentSpellTypes.CURRENT_CHANNELED_SPELL, OK: true);
            }
        }
    }

    public void CalculateMineRemaning(bool Force = false)
    {
        if (Type != GameObjectType.GAMEOBJECT_TYPE_CHEST || !loot.Locks.ContainsKey(LockID))
        {
            return;
        }
        var i = 0;
        checked
        {
            do
            {
                if (loot.Locks[LockID].KeyType[i] is 2 and (3 or 2))
                {
                    if (Force || MineRemaining == 0)
                    {
                        MineRemaining = WorldState.Rnd.Next((int)GetSound(4), (int)(GetSound(5) + 1L));
                    }
                    break;
                }
                i++;
            }
            while (i <= 4);
        }
    }

    public void SpawnAnimation()
    {
        Packets.PacketClass packet = new(Opcodes.SMSG_GAMEOBJECT_SPAWN_ANIM);
        packet.AddUInt64(GUID);
        SendToNearPlayers(ref packet);
        packet.Dispose();
    }

    public void TurnTo(ref WS_Base.BaseObject Target)
    {
        TurnTo(Target.positionX, Target.positionY);
    }

    public void TurnTo(float x, float y)
    {
        orientation = combat.GetOrientation(positionX, x, positionY, y);
        Rotations[2] = (float)Math.Sin(orientation / 2f);
        Rotations[3] = (float)Math.Cos(orientation / 2f);
        if (SeenBy.Count > 0)
        {
            Packets.PacketClass packet = new(Opcodes.SMSG_UPDATE_OBJECT);
            packet.AddInt32(2);
            packet.AddInt8(0);
            var tmpUpdate = updateClassFactory.Create(MangosGlobalConstants.FIELD_MASK_SIZE_GAMEOBJECT);
            tmpUpdate.SetUpdateFlag(18, orientation);
            tmpUpdate.SetUpdateFlag(10, Rotations[0]);
            tmpUpdate.SetUpdateFlag(11, Rotations[1]);
            tmpUpdate.SetUpdateFlag(12, Rotations[2]);
            tmpUpdate.SetUpdateFlag(13, Rotations[3]);
            var updateObject = this;
            tmpUpdate.AddToPacket(ref packet, ObjectUpdateType.UPDATETYPE_VALUES, ref updateObject);
            tmpUpdate.Dispose();
            SendToNearPlayers(ref packet);
            packet.Dispose();
        }
    }
}
