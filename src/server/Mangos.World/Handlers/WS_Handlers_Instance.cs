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

using Mangos.Common.Enums.Global;
using Mangos.Common.Enums.Map;
using Mangos.Common.Legacy;
using Mangos.Common.Legacy.Databases;
using Mangos.World.Maps;
using Mangos.World.Network;
using Mangos.World.Objects;
using Mangos.World.Objects.Factories.Maps;
using Mangos.World.Player;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections;
using System.Data;
using System.IO;

namespace Mangos.World.Handlers;

public class WS_Handlers_Instance
{
    private readonly ILogger<WS_Handlers_Instance> logger;
    private readonly WorldState worldState;
    private readonly CharacterDatabase characterDatabase;
    private readonly WS_GameObjects gameObjects;
    private readonly WS_Spawns spawns;
    private readonly MapTileFactory mapTileFactory;

    public WS_Handlers_Instance(
        ILogger<WS_Handlers_Instance> logger,
        WorldState worldState,
        CharacterDatabase characterDatabase,
        WS_GameObjects gameObjects,
        WS_Spawns spawns,
        MapTileFactory mapTileFactory)
    {
        this.logger = logger;
        this.worldState = worldState;
        this.characterDatabase = characterDatabase;
        this.gameObjects = gameObjects;
        this.spawns = spawns;
        this.mapTileFactory = mapTileFactory;
    }

    public void InstanceMapUpdate(WS_Maps maps)
    {
        DataTable q = new();
        var TimeStamp = Globals.Functions.GetTimestamp(DateAndTime.Now);
        characterDatabase.Query($"SELECT * FROM characters_instances WHERE expire < {TimeStamp};", ref q);
        IEnumerator enumerator = default;
        try
        {
            enumerator = q.Rows.GetEnumerator();
            while (enumerator.MoveNext())
            {
                DataRow row = (DataRow)enumerator.Current;
                if (maps.Maps.ContainsKey(row.As<uint>("map")))
                {
                    InstanceMapExpire(maps, row.As<uint>("map"), row.As<uint>("instance"));
                }
            }
        }
        finally
        {
            if (enumerator is IDisposable)
            {
                (enumerator as IDisposable).Dispose();
            }
        }
    }

    public uint InstanceMapCreate(uint Map)
    {
        DataTable q = new();
        characterDatabase.Query($"SELECT MAX(instance) FROM characters_instances WHERE map = {Map};", ref q);
        return q.Rows[0][0] != DBNull.Value ? (uint)(q.Rows[0].As<int>(0) + 1) : 0u;
    }

    public void InstanceMapSpawn(WS_Maps maps, uint map, uint instance)
    {
        checked
        {
            short x = 0;
            do
            {
                short y = 0;
                do
                {
                    if (!maps.Maps[map].TileUsed[x, y] && File.Exists(string.Format("maps\\{0}{1}{2}.map", Strings.Format(map, "000"), Strings.Format(x, "00"), Strings.Format(y, "00"))))
                    {
                        logger.LogInformation("Loading map [{2}: {0},{1}]...", x, y, map);
                        maps.Maps[map].TileUsed[x, y] = true;
                        maps.Maps[map].Tiles[x, y] = mapTileFactory.Create((byte)x, (byte)y, map, spawns);
                    }
                    if (maps.Maps[map].Tiles[x, y] != null)
                    {
                        spawns.LoadSpawns((byte)x, (byte)y, map, instance);
                    }
                    y = (short)unchecked(y + 1);
                }
                while (y <= 63);
                x = (short)unchecked(x + 1);
            }
            while (x <= 63);
        }
    }

    public void InstanceMapExpire(WS_Maps maps, uint map, uint instance)
    {
        checked
        {
            try
            {
                short x = 0;
                var empty = true;
                do
                {
                    short y = 0;
                    do
                    {
                        if (maps.Maps[map].Tiles[x, y] != null)
                        {
                            foreach (var GUID7 in maps.Maps[map].Tiles[x, y].PlayersHere.ToArray())
                            {
                                if (worldState.Characters[GUID7].instance == instance)
                                {
                                    empty = false;
                                    break;
                                }
                            }
                        }
                        if (!empty)
                        {
                            break;
                        }
                        y = (short)unchecked(y + 1);
                    }
                    while (y <= 63);
                    if (!empty)
                    {
                        break;
                    }
                    x = (short)unchecked(x + 1);
                }
                while (x <= 63);
                if (empty)
                {
                    characterDatabase.Update($"DELETE FROM characters_instances WHERE instance = {instance} AND map = {map};");
                    characterDatabase.Update($"DELETE FROM characters_instances_group WHERE instance = {instance} AND map = {map};");
                    short x3 = 0;
                    do
                    {
                        short y3 = 0;
                        do
                        {
                            if (maps.Maps[map].Tiles[x3, y3] != null)
                            {
                                foreach (var GUID3 in maps.Maps[map].Tiles[x3, y3].CreaturesHere.ToArray())
                                {
                                    if (worldState.WorldCreatures[GUID3].instance == instance)
                                    {
                                        worldState.WorldCreatures[GUID3].Destroy();
                                    }
                                }
                                foreach (var GUID4 in maps.Maps[map].Tiles[x3, y3].GameObjectsHere.ToArray())
                                {
                                    if (worldState.WorldGameObjects[GUID4].instance == instance)
                                    {
                                        gameObjects.Destroy(worldState.WorldGameObjects[GUID4]);
                                    }
                                }
                                foreach (var GUID5 in maps.Maps[map].Tiles[x3, y3].CorpseObjectsHere.ToArray())
                                {
                                    if (worldState.WorldCorpseObjects[GUID5].instance == instance)
                                    {
                                        worldState.WorldCorpseObjects[GUID5].Destroy();
                                    }
                                }
                                foreach (var GUID6 in maps.Maps[map].Tiles[x3, y3].DynamicObjectsHere.ToArray())
                                {
                                    if (worldState.WorldDynamicObjects[GUID6].instance == instance)
                                    {
                                        worldState.WorldDynamicObjects[GUID6].Delete();
                                    }
                                }
                            }
                            y3 = (short)unchecked(y3 + 1);
                        }
                        while (y3 <= 63);
                        x3 = (short)unchecked(x3 + 1);
                    }
                    while (x3 <= 63);
                    return;
                }
                characterDatabase.Update(string.Format("UPDATE characters_instances SET expire = {2} WHERE instance = {0} AND map = {1};", instance, map, Globals.Functions.GetTimestamp(DateAndTime.Now) + maps.Maps[map].ResetTime));
                characterDatabase.Update(string.Format("UPDATE characters_instances_group SET expire = {2} WHERE instance = {0} AND map = {1};", instance, map, Globals.Functions.GetTimestamp(DateAndTime.Now) + maps.Maps[map].ResetTime));
                short x2 = 0;
                do
                {
                    short y2 = 0;
                    do
                    {
                        if (maps.Maps[map].Tiles[x2, y2] != null)
                        {
                            foreach (var GUID in maps.Maps[map].Tiles[x2, y2].CreaturesHere.ToArray())
                            {
                                if (worldState.WorldCreatures[GUID].instance == instance)
                                {
                                    worldState.WorldCreatures[GUID].Respawn();
                                }
                            }
                            foreach (var GUID2 in maps.Maps[map].Tiles[x2, y2].GameObjectsHere.ToArray())
                            {
                                if (worldState.WorldGameObjects[GUID2].instance == instance)
                                {
                                    gameObjects.Respawn(worldState.WorldGameObjects[GUID2]);
                                    //worldState.WorldGameObjects[GUID2].Respawn(worldState.WorldGameObjects[GUID2]);
                                }
                            }
                        }
                        y2 = (short)unchecked(y2 + 1);
                    }
                    while (y2 <= 63);
                    x2 = (short)unchecked(x2 + 1);
                }
                while (x2 <= 63);
            }
            catch (Exception ex)
            {
                logger.LogCritical("Error expiring map instance.{0}{1}", Environment.NewLine, ex.ToString());
            }
        }
    }

    public void InstanceMapEnter(CharacterObject objCharacter, WS_Maps maps)
    {
        if (maps.Maps[objCharacter.MapID].Type == MapTypes.MAP_COMMON)
        {
            objCharacter.instance = 0u;
            objCharacter.SystemMessage(Globals.Functions.SetColor("You are not in instance.", 200, 0, byte.MaxValue));
            return;
        }
        InstanceMapUpdate(maps);
        DataTable q = new();
        characterDatabase.Query($"SELECT * FROM characters_instances WHERE char_guid = {objCharacter.GUID} AND map = {objCharacter.MapID};", ref q);
        if (q.Rows.Count > 0)
        {
            objCharacter.instance = Conversions.ToUInteger(q.Rows[0]["instance"]);
            objCharacter.SystemMessage(Globals.Functions.SetColor($"You are in instance #{objCharacter.instance}, map {objCharacter.MapID}", 0, 200, byte.MaxValue));
            SendInstanceMessage(ref objCharacter.client, objCharacter.MapID, Conversions.ToInteger(Operators.SubtractObject(q.Rows[0]["expire"], Globals.Functions.GetTimestamp(DateAndTime.Now))));
            return;
        }
        if (objCharacter.IsInGroup)
        {
            characterDatabase.Query($"SELECT * FROM characters_instances_group WHERE group_id = {objCharacter.Group.ID} AND map = {objCharacter.MapID};", ref q);
            if (q.Rows.Count > 0)
            {
                objCharacter.instance = Conversions.ToUInteger(q.Rows[0]["instance"]);
                objCharacter.SystemMessage(Globals.Functions.SetColor($"You are in instance #{objCharacter.instance}, map {objCharacter.MapID}", 245, 245, byte.MaxValue));
                SendInstanceMessage(ref objCharacter.client, objCharacter.MapID, Conversions.ToInteger(Operators.SubtractObject(q.Rows[0]["expire"], Globals.Functions.GetTimestamp(DateAndTime.Now))));
                return;
            }
        }
        checked
        {
            var instanceNewID = (int)InstanceMapCreate(objCharacter.MapID);
            var instanceNewResetTime = (int)(Globals.Functions.GetTimestamp(DateAndTime.Now) + maps.Maps[objCharacter.MapID].ResetTime);
            objCharacter.instance = (uint)instanceNewID;
            if (objCharacter.IsInGroup)
            {
                characterDatabase.Update($"INSERT INTO characters_instances_group (group_id, map, instance, expire) VALUES ({objCharacter.Group.ID}, {objCharacter.MapID}, {instanceNewID}, {instanceNewResetTime});");
            }
            InstanceMapSpawn(maps, objCharacter.MapID, (uint)instanceNewID);
            objCharacter.SystemMessage(Globals.Functions.SetColor($"You are in instance #{objCharacter.instance}, map {objCharacter.MapID}", 245, 245, byte.MaxValue));
            SendInstanceMessage(ref objCharacter.client, objCharacter.MapID, (int)(Globals.Functions.GetTimestamp(DateAndTime.Now) - instanceNewResetTime));
        }
    }

    public void InstanceUpdate(uint Map, uint Instance, uint Cleared)
    {
    }

    public void InstanceMapLeave(CharacterObject objChar)
    {
    }

    public void SendResetInstanceSuccess(ref WS_Network.ClientClass client, uint Map)
    {
    }

    public void SendResetInstanceFailed(ref WS_Network.ClientClass client, uint Map, ResetFailedReason Reason)
    {
    }

    public void SendResetInstanceFailedNotify(ref WS_Network.ClientClass client, uint Map)
    {
    }

    private void SendUpdateInstanceOwnership(ref WS_Network.ClientClass client, uint Saved)
    {
        logger.LogDebug("[{0}:{1}] SMSG_UPDATE_INSTANCE_OWNERSHIP", client.IP, client.Port);
    }

    private void SendUpdateLastInstance(ref WS_Network.ClientClass client, uint Map)
    {
        logger.LogDebug("[{0}:{1}] SMSG_UPDATE_LAST_INSTANCE", client.IP, client.Port);
    }

    public void SendInstanceSaved(CharacterObject Character)
    {
        DataTable q = new();
        characterDatabase.Query($"SELECT * FROM characters_instances WHERE char_guid = {Character.GUID};", ref q);
        SendUpdateInstanceOwnership(ref Character.client, 0u - ((q.Rows.Count > 0) ? 1u : 0u));
        IEnumerator enumerator = default;
        try
        {
            enumerator = q.Rows.GetEnumerator();
            while (enumerator.MoveNext())
            {
                DataRow row = (DataRow)enumerator.Current;
                SendUpdateLastInstance(ref Character.client, row.As<uint>("map"));
            }
        }
        finally
        {
            if (enumerator is IDisposable)
            {
                (enumerator as IDisposable).Dispose();
            }
        }
    }

    public static void SendInstanceMessage(ref WS_Network.ClientClass client, uint Map, int Time)
    {
        if (Time < 0)
        {
            _ = checked(-Time); // Time Discard (Unused)
        }
        else if (Time is <= 60 or >= 3600)
        {
            switch (Time)
            {
                default:
                    break;
            }
        }
    }
}
