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

using Mangos.Common.Globals;
using Mangos.Common.Legacy.Globals;
using Mangos.World.Globals;
using Mangos.World.Maps;
using Mangos.World.Objects;
using Mangos.World.Objects.Factories.Packets;
using Mangos.World.Player;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Mangos.World.Services;
public class CellUpdater(
    ILogger<CellUpdater> logger,
    WorldState worldState,
    WS_Maps maps,
    IMapTileLoader mapTileLoader,
    UpdateClassFactory updateClassFactory)
: ICellUpdater
{
    public void MoveCell(ref CharacterObject Character)
    {
        var oldX = Character.CellX;
        var oldY = Character.CellY;
        maps.GetMapTile(Character.positionX, Character.positionY, ref Character.CellX, ref Character.CellY);
        if (maps.Maps[Character.MapID].Tiles[Character.CellX, Character.CellY] == null)
        {
            mapTileLoader.LoadMap(Character.CellX, Character.CellY, Character.MapID);
        }
        if ((Character.CellX != oldX) || (Character.CellY != oldY) && Character != null)
        {
            maps.Maps[Character.MapID].Tiles?[oldX, oldY].PlayersHere.Remove(Character.GUID);
            maps.Maps[Character.MapID].Tiles[Character.CellX, Character.CellY].PlayersHere.Add(Character.GUID);
        }
    }

    public void UpdateCell(ref CharacterObject Character)
    {
        var list = Character.playersNear.ToArray();
        var array = list;
        foreach (var GUID in array)
        {
            var obj = Character;
            Dictionary<ulong, CharacterObject> cHARACTERs;
            ulong key;
            WS_Base.BaseObject objCharacter = (cHARACTERs = worldState.Characters)[key = GUID];
            var flag = obj.CanSee(ref objCharacter);
            cHARACTERs[key] = (CharacterObject)objCharacter;
            if (!flag)
            {
                Character.guidsForRemoving_Lock.AcquireWriterLock(MangosGlobalConstants.DEFAULT_LOCK_TIMEOUT);
                Character.guidsForRemoving.Add(GUID);
                Character.guidsForRemoving_Lock.ReleaseWriterLock();
                worldState.Characters[GUID].SeenBy.Remove(Character.GUID);
                Character.playersNear.Remove(GUID);
            }
            var characterObject = worldState.Characters[GUID];
            objCharacter = Character;
            flag = characterObject.CanSee(ref objCharacter);
            Character = (CharacterObject)objCharacter;
            if (!flag && Character.SeenBy.Contains(GUID))
            {
                worldState.Characters[GUID].guidsForRemoving_Lock.AcquireWriterLock(MangosGlobalConstants.DEFAULT_LOCK_TIMEOUT);
                worldState.Characters[GUID].guidsForRemoving.Add(Character.GUID);
                worldState.Characters[GUID].guidsForRemoving_Lock.ReleaseWriterLock();
                Character.SeenBy.Remove(GUID);
                worldState.Characters[GUID].playersNear.Remove(Character.GUID);
            }
        }
        list = Character.creaturesNear.ToArray();
        var array2 = list;
        foreach (var GUID2 in array2)
        {
            int num;
            if (worldState.WorldCreatures.ContainsKey(GUID2))
            {
                var obj2 = Character;
                Dictionary<ulong, WS_Creatures.CreatureObject> wORLD_CREATUREs;
                ulong key;
                WS_Base.BaseObject objCharacter = (wORLD_CREATUREs = worldState.WorldCreatures)[key = GUID2];
                var flag = obj2.CanSee(ref objCharacter);
                wORLD_CREATUREs[key] = (WS_Creatures.CreatureObject)objCharacter;
                num = (!flag) ? 1 : 0;
            }
            else
            {
                num = 1;
            }
            if (num != 0)
            {
                Character.guidsForRemoving_Lock.AcquireWriterLock(MangosGlobalConstants.DEFAULT_LOCK_TIMEOUT);
                Character.guidsForRemoving.Add(GUID2);
                Character.guidsForRemoving_Lock.ReleaseWriterLock();
                worldState.WorldCreatures[GUID2].SeenBy.Remove(Character.GUID);
                Character.creaturesNear.Remove(GUID2);
            }
        }
        list = Character.gameObjectsNear.ToArray();
        var array3 = list;
        foreach (var GUID3 in array3)
        {
            if (LegacyGlobalFunctions.GuidIsMoTransport(GUID3))
            {
                var obj3 = Character;
                Dictionary<ulong, WS_Transports.TransportObject> wORLD_TRANSPORTs;
                ulong key;
                WS_Base.BaseObject objCharacter = (wORLD_TRANSPORTs = worldState.WorldTransports)[key = GUID3];
                var flag = obj3.CanSee(ref objCharacter);
                wORLD_TRANSPORTs[key] = (WS_Transports.TransportObject)objCharacter;
                if (!flag)
                {
                    Character.guidsForRemoving_Lock.AcquireWriterLock(MangosGlobalConstants.DEFAULT_LOCK_TIMEOUT);
                    Character.guidsForRemoving.Add(GUID3);
                    Character.guidsForRemoving_Lock.ReleaseWriterLock();
                    worldState.WorldTransports[GUID3].SeenBy.Remove(Character.GUID);
                    Character.gameObjectsNear.Remove(GUID3);
                }
            }
            else
            {
                var obj4 = Character;
                Dictionary<ulong, GameObject> wORLD_GAMEOBJECTs;
                ulong key;
                WS_Base.BaseObject objCharacter = (wORLD_GAMEOBJECTs = worldState.WorldGameObjects)[key = GUID3];
                var flag = obj4.CanSee(ref objCharacter);
                wORLD_GAMEOBJECTs[key] = (GameObject)objCharacter;
                if (!flag)
                {
                    Character.guidsForRemoving_Lock.AcquireWriterLock(MangosGlobalConstants.DEFAULT_LOCK_TIMEOUT);
                    Character.guidsForRemoving.Add(GUID3);
                    Character.guidsForRemoving_Lock.ReleaseWriterLock();
                    worldState.WorldGameObjects[GUID3].SeenBy.Remove(Character.GUID);
                    Character.gameObjectsNear.Remove(GUID3);
                }
            }
        }
        list = Character.dynamicObjectsNear.ToArray();
        var array4 = list;
        foreach (var GUID4 in array4)
        {
            var obj5 = Character;
            Dictionary<ulong, WS_DynamicObjects.DynamicObject> wORLD_DYNAMICOBJECTs;
            ulong key;
            WS_Base.BaseObject objCharacter = (wORLD_DYNAMICOBJECTs = worldState.WorldDynamicObjects)[key = GUID4];
            var flag = obj5.CanSee(ref objCharacter);
            wORLD_DYNAMICOBJECTs[key] = (WS_DynamicObjects.DynamicObject)objCharacter;
            if (!flag)
            {
                Character.guidsForRemoving_Lock.AcquireWriterLock(MangosGlobalConstants.DEFAULT_LOCK_TIMEOUT);
                Character.guidsForRemoving.Add(GUID4);
                Character.guidsForRemoving_Lock.ReleaseWriterLock();
                worldState.WorldDynamicObjects[GUID4].SeenBy.Remove(Character.GUID);
                Character.dynamicObjectsNear.Remove(GUID4);
            }
        }
        list = Character.corpseObjectsNear.ToArray();
        var array5 = list;
        foreach (var GUID5 in array5)
        {
            var obj6 = Character;
            Dictionary<ulong, WS_Corpses.CorpseObject> wORLD_CORPSEOBJECTs;
            ulong key;
            WS_Base.BaseObject objCharacter = (wORLD_CORPSEOBJECTs = worldState.WorldCorpseObjects)[key = GUID5];
            var flag = obj6.CanSee(ref objCharacter);
            wORLD_CORPSEOBJECTs[key] = (WS_Corpses.CorpseObject)objCharacter;
            if (!flag)
            {
                Character.guidsForRemoving_Lock.AcquireWriterLock(MangosGlobalConstants.DEFAULT_LOCK_TIMEOUT);
                Character.guidsForRemoving.Add(GUID5);
                Character.guidsForRemoving_Lock.ReleaseWriterLock();
                worldState.WorldCorpseObjects[GUID5].SeenBy.Remove(Character.GUID);
                Character.corpseObjectsNear.Remove(GUID5);
            }
        }
        short CellXAdd = -1;
        short CellYAdd = -1;
        if (maps.GetSubMapTileX(Character.positionX) > 32)
        {
            CellXAdd = 1;
        }
        if (maps.GetSubMapTileX(Character.positionY) > 32)
        {
            CellYAdd = 1;
        }
        checked
        {
            if ((short)(Character.CellX + CellXAdd) is > 63 or < 0)
            {
                CellXAdd = 0;
            }
            if ((short)(Character.CellY + CellYAdd) is > 63 or < 0)
            {
                CellYAdd = 0;
            }
            if (maps.Maps[Character.MapID].Tiles[Character.CellX, Character.CellY] == null)
            {
                mapTileLoader.LoadMap(Character.CellX, Character.CellY, Character.MapID);
            }
            if (maps.Maps[Character.MapID].Tiles[Character.CellX, Character.CellY].CreaturesHere.Count > 0 || maps.Maps[Character.MapID].Tiles[Character.CellX, Character.CellY].GameObjectsHere.Count > 0)
            {
                UpdateCreaturesAndGameObjectsInCell(ref maps.Maps[Character.MapID].Tiles[Character.CellX, Character.CellY], ref Character);
            }
            if (maps.Maps[Character.MapID].Tiles[Character.CellX, Character.CellY].PlayersHere.Count > 0)
            {
                UpdatePlayersInCell(ref maps.Maps[Character.MapID].Tiles[Character.CellX, Character.CellY], ref Character);
            }
            if (maps.Maps[Character.MapID].Tiles[Character.CellX, Character.CellY].CorpseObjectsHere.Count > 0)
            {
                UpdateCorpseObjectsInCell(ref maps.Maps[Character.MapID].Tiles[Character.CellX, Character.CellY], ref Character);
            }
            if (CellXAdd != 0)
            {
                if (maps.Maps[Character.MapID].Tiles[(short)unchecked(Character.CellX + CellXAdd), Character.CellY] == null)
                {
                    mapTileLoader.LoadMap((byte)(short)(Character.CellX + CellXAdd), Character.CellY, Character.MapID);
                }
                if (maps.Maps[Character.MapID].Tiles[(short)(Character.CellX + CellXAdd), Character.CellY].CreaturesHere.Count > 0 || maps.Maps[Character.MapID].Tiles[(short)unchecked(Character.CellX + CellXAdd), Character.CellY].GameObjectsHere.Count > 0)
                {
                    UpdateCreaturesAndGameObjectsInCell(ref maps.Maps[Character.MapID].Tiles[(short)(Character.CellX + CellXAdd), Character.CellY], ref Character);
                }
                if (maps.Maps[Character.MapID].Tiles[(short)(Character.CellX + CellXAdd), Character.CellY].PlayersHere.Count > 0)
                {
                    UpdatePlayersInCell(ref maps.Maps[Character.MapID].Tiles[(short)(Character.CellX + CellXAdd), Character.CellY], ref Character);
                }
                if (maps.Maps[Character.MapID].Tiles[(short)(Character.CellX + CellXAdd), Character.CellY].CorpseObjectsHere.Count > 0)
                {
                    UpdateCorpseObjectsInCell(ref maps.Maps[Character.MapID].Tiles[(short)(Character.CellX + CellXAdd), Character.CellY], ref Character);
                }
            }
            if (CellYAdd != 0)
            {
                if (maps.Maps[Character.MapID].Tiles[Character.CellX, (short)(Character.CellY + CellYAdd)] == null)
                {
                    mapTileLoader.LoadMap(Character.CellX, (byte)(short)unchecked(Character.CellY + CellYAdd), Character.MapID);
                }
                if (maps.Maps[Character.MapID].Tiles[Character.CellX, (short)unchecked(Character.CellY + CellYAdd)].CreaturesHere.Count > 0 || maps.Maps[Character.MapID].Tiles[Character.CellX, (short)unchecked(Character.CellY + CellYAdd)].GameObjectsHere.Count > 0)
                {
                    UpdateCreaturesAndGameObjectsInCell(ref maps.Maps[Character.MapID].Tiles[Character.CellX, (short)unchecked(Character.CellY + CellYAdd)], ref Character);
                }
                if (maps.Maps[Character.MapID].Tiles[Character.CellX, (short)unchecked(Character.CellY + CellYAdd)].PlayersHere.Count > 0)
                {
                    UpdatePlayersInCell(ref maps.Maps[Character.MapID].Tiles[Character.CellX, (short)unchecked(Character.CellY + CellYAdd)], ref Character);
                }
                if (maps.Maps[Character.MapID].Tiles[Character.CellX, (short)unchecked(Character.CellY + CellYAdd)].CorpseObjectsHere.Count > 0)
                {
                    UpdateCorpseObjectsInCell(ref maps.Maps[Character.MapID].Tiles[Character.CellX, (short)unchecked(Character.CellY + CellYAdd)], ref Character);
                }
            }
            if (CellYAdd != 0 && CellXAdd != 0)
            {
                if (maps.Maps[Character.MapID].Tiles[(short)unchecked(Character.CellX + CellXAdd), (short)unchecked(Character.CellY + CellYAdd)] == null)
                {
                    mapTileLoader.LoadMap((byte)(short)unchecked(Character.CellX + CellXAdd), (byte)(short)unchecked(Character.CellY + CellYAdd), Character.MapID);
                }
                if (maps.Maps[Character.MapID].Tiles[(short)unchecked(Character.CellX + CellXAdd), (short)unchecked(Character.CellY + CellYAdd)].CreaturesHere.Count > 0 || maps.Maps[Character.MapID].Tiles[(short)unchecked(Character.CellX + CellXAdd), (short)unchecked(Character.CellY + CellYAdd)].GameObjectsHere.Count > 0)
                {
                    UpdateCreaturesAndGameObjectsInCell(ref maps.Maps[Character.MapID].Tiles[(short)unchecked(Character.CellX + CellXAdd), (short)unchecked(Character.CellY + CellYAdd)], ref Character);
                }
                if (maps.Maps[Character.MapID].Tiles[(short)unchecked(Character.CellX + CellXAdd), (short)unchecked(Character.CellY + CellYAdd)].PlayersHere.Count > 0)
                {
                    UpdatePlayersInCell(ref maps.Maps[Character.MapID].Tiles[(short)unchecked(Character.CellX + CellXAdd), (short)unchecked(Character.CellY + CellYAdd)], ref Character);
                }
                if (maps.Maps[Character.MapID].Tiles[(short)unchecked(Character.CellX + CellXAdd), (short)unchecked(Character.CellY + CellYAdd)].CorpseObjectsHere.Count > 0)
                {
                    UpdateCorpseObjectsInCell(ref maps.Maps[Character.MapID].Tiles[(short)unchecked(Character.CellX + CellXAdd), (short)unchecked(Character.CellY + CellYAdd)], ref Character);
                }
            }
            Character.SendOutOfRangeUpdate();
        }
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void UpdatePlayersInCell(ref WS_Maps.TMapTile MapTile, ref CharacterObject Character)
    {
        var tMapTile = MapTile;
        var list = tMapTile.PlayersHere.ToArray();
        var array = list;
        foreach (var GUID in array)
        {
            if (!worldState.Characters[GUID].SeenBy.Contains(Character.GUID))
            {
                var obj = Character;
                Dictionary<ulong, CharacterObject> cHARACTERs;
                ulong key;
                WS_Base.BaseObject objCharacter = (cHARACTERs = worldState.Characters)[key = GUID];
                var flag = obj.CanSee(ref objCharacter);
                cHARACTERs[key] = (CharacterObject)objCharacter;
                if (flag)
                {
                    Packets.PacketClass packet = new(Opcodes.SMSG_UPDATE_OBJECT);
                    packet.AddInt32(1);
                    packet.AddInt8(0);
                    var tmpUpdate = updateClassFactory.Create(MangosGlobalConstants.FIELD_MASK_SIZE_PLAYER);
                    worldState.Characters[GUID].FillAllUpdateFlags(ref tmpUpdate);
                    var updateClass = tmpUpdate;
                    var updateObject = (cHARACTERs = worldState.Characters)[key = GUID];
                    updateClass.AddToPacket(ref packet, ObjectUpdateType.UPDATETYPE_CREATE_OBJECT, ref updateObject);
                    cHARACTERs[key] = updateObject;
                    tmpUpdate.Dispose();
                    Character.client.Send(ref packet);
                    packet.Dispose();
                    worldState.Characters[GUID].SeenBy.Add(Character.GUID);
                    Character.playersNear.Add(GUID);
                }
            }
            if (!Character.SeenBy.Contains(GUID))
            {
                var characterObject = worldState.Characters[GUID];
                WS_Base.BaseObject objCharacter = Character;
                var flag = characterObject.CanSee(ref objCharacter);
                Character = (CharacterObject)objCharacter;
                if (flag)
                {
                    Packets.PacketClass myPacket = new(Opcodes.SMSG_UPDATE_OBJECT);
                    myPacket.AddInt32(1);
                    myPacket.AddInt8(0);
                    var myTmpUpdate = updateClassFactory.Create(MangosGlobalConstants.FIELD_MASK_SIZE_PLAYER);
                    Character.FillAllUpdateFlags(ref myTmpUpdate);
                    myTmpUpdate.AddToPacket(ref myPacket, ObjectUpdateType.UPDATETYPE_CREATE_OBJECT, ref Character);
                    myTmpUpdate.Dispose();
                    worldState.Characters[GUID].client.Send(ref myPacket);
                    myPacket.Dispose();
                    Character.SeenBy.Add(GUID);
                    worldState.Characters[GUID].playersNear.Add(Character.GUID);
                }
            }
        }
    }

    public void UpdateCreaturesAndGameObjectsInCell(ref WS_Maps.TMapTile MapTile, ref CharacterObject Character)
    {
        Packets.UpdatePacketClass packet = new();
        var tMapTile = MapTile;
        var list = tMapTile.CreaturesHere.ToArray();
        var array = list;
        foreach (var GUID in array)
        {
            if (!Character.creaturesNear.Contains(GUID) && worldState.WorldCreatures.ContainsKey(GUID))
            {
                var obj = Character;
                Dictionary<ulong, WS_Creatures.CreatureObject> wORLD_CREATUREs;
                ulong key;
                WS_Base.BaseObject objCharacter = (wORLD_CREATUREs = worldState.WorldCreatures)[key = GUID];
                var flag = obj.CanSee(ref objCharacter);
                wORLD_CREATUREs[key] = (WS_Creatures.CreatureObject)objCharacter;
                if (flag)
                {
                    var tmpUpdate = updateClassFactory.Create(MangosGlobalConstants.FIELD_MASK_SIZE_UNIT);
                    worldState.WorldCreatures[GUID].FillAllUpdateFlags(ref tmpUpdate);
                    var updateClass = tmpUpdate;
                    Packets.PacketClass packet2 = packet;
                    var updateObject = (wORLD_CREATUREs = worldState.WorldCreatures)[key = GUID];
                    updateClass.AddToPacket(ref packet2, ObjectUpdateType.UPDATETYPE_CREATE_OBJECT, ref updateObject);
                    wORLD_CREATUREs[key] = updateObject;
                    packet = (Packets.UpdatePacketClass)packet2;
                    tmpUpdate.Dispose();
                    Character.creaturesNear.Add(GUID);
                    worldState.WorldCreatures[GUID].SeenBy.Add(Character.GUID);
                }
            }
        }
        list = tMapTile.GameObjectsHere.ToArray();
        var array2 = list;
        foreach (var GUID2 in array2)
        {
            if (Character.gameObjectsNear.Contains(GUID2))
            {
                continue;
            }
            if (LegacyGlobalFunctions.GuidIsMoTransport(GUID2))
            {
                var obj2 = Character;
                Dictionary<ulong, WS_Transports.TransportObject> wORLD_TRANSPORTs;
                ulong key;
                WS_Base.BaseObject objCharacter = (wORLD_TRANSPORTs = worldState.WorldTransports)[key = GUID2];
                var flag = obj2.CanSee(ref objCharacter);
                wORLD_TRANSPORTs[key] = (WS_Transports.TransportObject)objCharacter;
                if (flag)
                {
                    var tmpUpdate3 = updateClassFactory.Create(MangosGlobalConstants.FIELD_MASK_SIZE_GAMEOBJECT);
                    worldState.WorldTransports[GUID2].FillAllUpdateFlags(ref tmpUpdate3, ref Character);
                    var updateClass2 = tmpUpdate3;
                    Packets.PacketClass packet2 = packet;
                    GameObject updateObject2 = (wORLD_TRANSPORTs = worldState.WorldTransports)[key = GUID2];
                    updateClass2.AddToPacket(ref packet2, ObjectUpdateType.UPDATETYPE_CREATE_OBJECT, ref updateObject2);
                    wORLD_TRANSPORTs[key] = (WS_Transports.TransportObject)updateObject2;
                    packet = (Packets.UpdatePacketClass)packet2;
                    tmpUpdate3.Dispose();
                    Character.gameObjectsNear.Add(GUID2);
                    worldState.WorldTransports[GUID2].SeenBy.Add(Character.GUID);
                }
            }
            else
            {
                var obj3 = Character;
                Dictionary<ulong, GameObject> wORLD_GAMEOBJECTs;
                ulong key;
                WS_Base.BaseObject objCharacter = (wORLD_GAMEOBJECTs = worldState.WorldGameObjects)[key = GUID2];
                var flag = obj3.CanSee(ref objCharacter);
                wORLD_GAMEOBJECTs[key] = (GameObject)objCharacter;
                if (flag)
                {
                    var tmpUpdate2 = updateClassFactory.Create(MangosGlobalConstants.FIELD_MASK_SIZE_GAMEOBJECT);
                    worldState.WorldGameObjects[GUID2].FillAllUpdateFlags(ref tmpUpdate2, ref Character);
                    var updateClass3 = tmpUpdate2;
                    Packets.PacketClass packet2 = packet;
                    var updateObject2 = (wORLD_GAMEOBJECTs = worldState.WorldGameObjects)[key = GUID2];
                    updateClass3.AddToPacket(ref packet2, ObjectUpdateType.UPDATETYPE_CREATE_OBJECT, ref updateObject2);
                    wORLD_GAMEOBJECTs[key] = updateObject2;
                    packet = (Packets.UpdatePacketClass)packet2;
                    tmpUpdate2.Dispose();
                    Character.gameObjectsNear.Add(GUID2);
                    worldState.WorldGameObjects[GUID2].SeenBy.Add(Character.GUID);
                }
            }
        }
        list = tMapTile.DynamicObjectsHere.ToArray();
        var array3 = list;
        foreach (var GUID3 in array3)
        {
            if (!Character.dynamicObjectsNear.Contains(GUID3))
            {
                var obj4 = Character;
                Dictionary<ulong, WS_DynamicObjects.DynamicObject> wORLD_DYNAMICOBJECTs;
                ulong key;
                WS_Base.BaseObject objCharacter = (wORLD_DYNAMICOBJECTs = worldState.WorldDynamicObjects)[key = GUID3];
                var flag = obj4.CanSee(ref objCharacter);
                wORLD_DYNAMICOBJECTs[key] = (WS_DynamicObjects.DynamicObject)objCharacter;
                if (flag)
                {
                    var tmpUpdate4 = updateClassFactory.Create(MangosGlobalConstants.FIELD_MASK_SIZE_DYNAMICOBJECT);
                    worldState.WorldDynamicObjects[GUID3].FillAllUpdateFlags(ref tmpUpdate4);
                    var updateClass4 = tmpUpdate4;
                    Packets.PacketClass packet2 = packet;
                    var updateObject3 = (wORLD_DYNAMICOBJECTs = worldState.WorldDynamicObjects)[key = GUID3];
                    updateClass4.AddToPacket(ref packet2, ObjectUpdateType.UPDATETYPE_CREATE_OBJECT_SELF, ref updateObject3);
                    wORLD_DYNAMICOBJECTs[key] = updateObject3;
                    packet = (Packets.UpdatePacketClass)packet2;
                    tmpUpdate4.Dispose();
                    Character.dynamicObjectsNear.Add(GUID3);
                    worldState.WorldDynamicObjects[GUID3].SeenBy.Add(Character.GUID);
                }
            }
        }

        if (packet.UpdatesCount > 0)
        {
            packet.CompressUpdatePacket();
            var client = Character.client;
            Packets.PacketClass packet2 = packet;
            client.Send(ref packet2);
            packet = (Packets.UpdatePacketClass)packet2;
        }
        packet.Dispose();
    }

    public void UpdateCreaturesInCell(ref WS_Maps.TMapTile MapTile, ref CharacterObject Character)
    {
        var tMapTile = MapTile;
        var list = tMapTile.CreaturesHere.ToArray();
        var array = list;
        foreach (var GUID in array)
        {
            if (!Character.creaturesNear.Contains(GUID))
            {
                var obj = Character;
                Dictionary<ulong, WS_Creatures.CreatureObject> wORLD_CREATUREs;
                ulong key;
                WS_Base.BaseObject objCharacter = (wORLD_CREATUREs = worldState.WorldCreatures)[key = GUID];
                var flag = obj.CanSee(ref objCharacter);
                wORLD_CREATUREs[key] = (WS_Creatures.CreatureObject)objCharacter;
                if (flag)
                {
                    Packets.PacketClass packet = new(Opcodes.SMSG_UPDATE_OBJECT);
                    packet.AddInt32(1);
                    packet.AddInt8(0);
                    var tmpUpdate = updateClassFactory.Create(MangosGlobalConstants.FIELD_MASK_SIZE_UNIT);
                    worldState.WorldCreatures[GUID].FillAllUpdateFlags(ref tmpUpdate);
                    var updateClass = tmpUpdate;
                    var updateObject = (wORLD_CREATUREs = worldState.WorldCreatures)[key = GUID];
                    updateClass.AddToPacket(ref packet, ObjectUpdateType.UPDATETYPE_CREATE_OBJECT, ref updateObject);
                    wORLD_CREATUREs[key] = updateObject;
                    tmpUpdate.Dispose();
                    Character.client.Send(ref packet);
                    packet.Dispose();
                    Character.creaturesNear.Add(GUID);
                    worldState.WorldCreatures[GUID].SeenBy.Add(Character.GUID);
                }
            }
        }
    }

    public void UpdateGameObjectsInCell(ref WS_Maps.TMapTile MapTile, ref CharacterObject Character)
    {
        var tMapTile = MapTile;
        var list = tMapTile.GameObjectsHere.ToArray();
        var array = list;
        foreach (var GUID in array)
        {
            if (!Character.gameObjectsNear.Contains(GUID))
            {
                int num;
                if (LegacyGlobalFunctions.GuidIsGameObject(GUID) && worldState.WorldGameObjects.ContainsKey(GUID))
                {
                    var obj = Character;
                    Dictionary<ulong, GameObject> wORLD_GAMEOBJECTs;
                    ulong key;
                    WS_Base.BaseObject objCharacter = (wORLD_GAMEOBJECTs = worldState.WorldGameObjects)[key = GUID];
                    var flag = obj.CanSee(ref objCharacter);
                    wORLD_GAMEOBJECTs[key] = (GameObject)objCharacter;
                    num = flag ? 1 : 0;
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
                    worldState.WorldGameObjects[GUID].FillAllUpdateFlags(ref tmpUpdate, ref Character);
                    var updateClass = tmpUpdate;
                    Dictionary<ulong, GameObject> wORLD_GAMEOBJECTs;
                    ulong key;
                    var updateObject = (wORLD_GAMEOBJECTs = worldState.WorldGameObjects)[key = GUID];
                    updateClass.AddToPacket(ref packet, ObjectUpdateType.UPDATETYPE_CREATE_OBJECT, ref updateObject);
                    wORLD_GAMEOBJECTs[key] = updateObject;
                    tmpUpdate.Dispose();
                    Character.client.Send(ref packet);
                    packet.Dispose();
                    Character.gameObjectsNear.Add(GUID);
                    worldState.WorldGameObjects[GUID].SeenBy.Add(Character.GUID);
                }
            }
        }
    }

    public void UpdateCorpseObjectsInCell(ref WS_Maps.TMapTile MapTile, ref CharacterObject Character)
    {
        var tMapTile = MapTile;
        var list = tMapTile.CorpseObjectsHere.ToArray();
        var array = list;
        foreach (var GUID in array)
        {
            if (!Character.corpseObjectsNear.Contains(GUID))
            {
                var obj = Character;
                Dictionary<ulong, WS_Corpses.CorpseObject> wORLD_CORPSEOBJECTs;
                ulong key;
                WS_Base.BaseObject objCharacter = (wORLD_CORPSEOBJECTs = worldState.WorldCorpseObjects)[key = GUID];
                var flag = obj.CanSee(ref objCharacter);
                wORLD_CORPSEOBJECTs[key] = (WS_Corpses.CorpseObject)objCharacter;
                if (flag)
                {
                    Packets.PacketClass packet = new(Opcodes.SMSG_UPDATE_OBJECT);
                    packet.AddInt32(1);
                    packet.AddInt8(0);
                    var tmpUpdate = updateClassFactory.Create(MangosGlobalConstants.FIELD_MASK_SIZE_CORPSE);
                    worldState.WorldCorpseObjects[GUID].FillAllUpdateFlags(ref tmpUpdate);
                    var updateClass = tmpUpdate;
                    var updateObject = (wORLD_CORPSEOBJECTs = worldState.WorldCorpseObjects)[key = GUID];
                    updateClass.AddToPacket(ref packet, ObjectUpdateType.UPDATETYPE_CREATE_OBJECT, ref updateObject);
                    wORLD_CORPSEOBJECTs[key] = updateObject;
                    tmpUpdate.Dispose();
                    Character.client.Send(ref packet);
                    packet.Dispose();
                    Character.corpseObjectsNear.Add(GUID);
                    worldState.WorldCorpseObjects[GUID].SeenBy.Add(Character.GUID);
                }
            }
        }
    }

    public void AddToWorld(ref CharacterObject Character)
    {
        maps.GetMapTile(Character.positionX, Character.positionY, ref Character.CellX, ref Character.CellY);
        if (maps.Maps[Character.MapID].Tiles[Character.CellX, Character.CellY] == null)
        {
            mapTileLoader.LoadMap(Character.CellX, Character.CellY, Character.MapID);
        }
        maps.Maps[Character.MapID].Tiles[Character.CellX, Character.CellY].PlayersHere.Add(Character.GUID);
        UpdateCell(ref Character);
        Character.Pet?.Spawn();
    }

    public void RemoveFromWorld(ref CharacterObject Character)
    {
        if (!maps.Maps.ContainsKey(Character.MapID))
        {
            return;
        }
        if (maps.Maps[Character.MapID].Tiles[Character.CellX, Character.CellY] != null)
        {
            try
            {
                maps.GetMapTile(Character.positionX, Character.positionY, ref Character.CellX, ref Character.CellY);
                maps.Maps[Character.MapID].Tiles[Character.CellX, Character.CellY].PlayersHere.Remove(Character.GUID);
            }
            catch (Exception ex2)
            {
                //ProjectData.SetProjectError(ex2);
                logger.LogError("Error removing character {0} from map", Character.Name);
                //ProjectData.ClearProjectError();
            }
        }
        var list = Character.SeenBy.ToArray();
        var array = list;
        foreach (var GUID in array)
        {
            if (worldState.Characters[GUID].playersNear.Contains(Character.GUID))
            {
                worldState.Characters[GUID].guidsForRemoving_Lock.AcquireWriterLock(MangosGlobalConstants.DEFAULT_LOCK_TIMEOUT);
                worldState.Characters[GUID].guidsForRemoving.Add(Character.GUID);
                worldState.Characters[GUID].guidsForRemoving_Lock.ReleaseWriterLock();
                worldState.Characters[GUID].playersNear.Remove(Character.GUID);
            }
            worldState.Characters[GUID].SeenBy.Remove(Character.GUID);
        }
        Character.playersNear.Clear();
        Character.SeenBy.Clear();
        list = Character.creaturesNear.ToArray();
        var array2 = list;
        foreach (var GUID2 in array2)
        {
            if (worldState.WorldCreatures[GUID2].SeenBy.Contains(Character.GUID))
            {
                worldState.WorldCreatures[GUID2].SeenBy.Remove(Character.GUID);
            }
        }
        Character.creaturesNear.Clear();
        list = Character.gameObjectsNear.ToArray();
        var array3 = list;
        foreach (var GUID3 in array3)
        {
            if (LegacyGlobalFunctions.GuidIsMoTransport(GUID3))
            {
                if (worldState.WorldTransports[GUID3].SeenBy.Contains(Character.GUID))
                {
                    worldState.WorldTransports[GUID3].SeenBy.Remove(Character.GUID);
                }
            }
            else if (worldState.WorldGameObjects[GUID3].SeenBy.Contains(Character.GUID))
            {
                worldState.WorldGameObjects[GUID3].SeenBy.Remove(Character.GUID);
            }
        }
        Character.gameObjectsNear.Clear();
        list = Character.corpseObjectsNear.ToArray();
        var array4 = list;
        foreach (var GUID4 in array4)
        {
            if (worldState.WorldCorpseObjects[GUID4].SeenBy.Contains(Character.GUID))
            {
                worldState.WorldCorpseObjects[GUID4].SeenBy.Remove(Character.GUID);
            }
        }
        Character.corpseObjectsNear.Clear();
        Character.Pet?.Hide();
    }
}
