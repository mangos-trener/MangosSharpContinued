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
using Mangos.Common.Legacy;
using Mangos.Common.Legacy.Databases;
using Mangos.World.Objects;
using Mangos.World.Objects.Factories;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections;
using System.Data;

namespace Mangos.World.Maps;
public class WS_Spawns(
    ILogger<WS_Spawns> logger,
    WorldState worldState,
    CharacterDatabase characterDatabase,
    WorldDatabase worldDatabase,
    WS_Maps maps,
    WS_GameObjects gameObjects,
    GameObjectFactory gameObjectFactory,
    CreatureObjectFactory creatureObjectFactory,
    CorpseObjectFactory corpseObjectFactory)
{
    private readonly WS_GameObjects gameObjects = gameObjects;

    public void LoadSpawns(byte TileX, byte TileY, uint TileMap, uint TileInstance)
    {
        checked
        {
            var MinX = (32 - TileX) * MangosGlobalConstants.SIZE;
            var MaxX = (32 - (TileX + 1)) * MangosGlobalConstants.SIZE;
            var MinY = (32 - TileY) * MangosGlobalConstants.SIZE;
            var MaxY = (32 - (TileY + 1)) * MangosGlobalConstants.SIZE;
            if (MinX > MaxX)
            {
                var tmpSng2 = MinX;
                MinX = MaxX;
                MaxX = tmpSng2;
            }
            if (MinY > MaxY)
            {
                var tmpSng = MinY;
                MinY = MaxY;
                MaxY = tmpSng;
            }
            var InstanceGuidAdd = 0uL;
            if (TileInstance > 0L)
            {
                InstanceGuidAdd = Convert.ToUInt64(decimal.Add(new decimal(1000000L), decimal.Multiply(new decimal(TileInstance - 1L), new decimal(100000L))));
            }
            DataTable MysqlQuery = new();
            worldDatabase.Query($"SELECT * FROM creature LEFT OUTER JOIN game_event_creature ON creature.guid = game_event_creature.guid WHERE map={TileMap} AND position_X BETWEEN '{MinX}' AND '{MaxX}' AND position_Y BETWEEN '{MinY}' AND '{MaxY}';", ref MysqlQuery);
            IEnumerator enumerator = default;
            try
            {
                enumerator = MysqlQuery.Rows.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    DataRow row = (DataRow)enumerator.Current;
                    if (worldState.WorldCreatures.ContainsKey(Convert.ToUInt64(decimal.Add(decimal.Add(new decimal(row.As<long>("guid")), new decimal(InstanceGuidAdd)), new decimal(MangosGlobalConstants.GUID_UNIT)))))
                    {
                        continue;
                    }
                    try
                    {
                        var tmpCr = creatureObjectFactory.Create(Convert.ToUInt64(decimal.Add(new decimal(row.As<long>("guid")), new decimal(InstanceGuidAdd))), row);
                        if (tmpCr.GameEvent == 0)
                        {
                            tmpCr.instance = TileInstance;
                            tmpCr.AddToWorld();
                        }
                    }
                    catch (Exception ex4)
                    {
                        logger.LogCritical("Error when creating creature [{0}].{1}{2}", row["id"], Environment.NewLine, ex4.ToString());
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
            MysqlQuery.Clear();
            worldDatabase.Query($"SELECT * FROM gameobject LEFT OUTER JOIN game_event_gameobject ON gameobject.guid = game_event_gameobject.guid WHERE map={TileMap} AND spawntimesecs>=0 AND position_X BETWEEN '{MinX}' AND '{MaxX}' AND position_Y BETWEEN '{MinY}' AND '{MaxY}';", ref MysqlQuery);
            IEnumerator enumerator2 = default;
            try
            {
                enumerator2 = MysqlQuery.Rows.GetEnumerator();
                while (enumerator2.MoveNext())
                {
                    DataRow row = (DataRow)enumerator2.Current;
                    if (worldState.WorldGameObjects.ContainsKey(row.As<ulong>("guid") + InstanceGuidAdd + MangosGlobalConstants.GUID_GAMEOBJECT) || worldState.WorldGameObjects.ContainsKey(row.As<ulong>("guid") + InstanceGuidAdd + MangosGlobalConstants.GUID_TRANSPORT))
                    {
                        continue;
                    }
                    try
                    {
                        var tmpGo = gameObjectFactory.Create(row.As<ulong>("guid") + InstanceGuidAdd, row);

                        if (tmpGo.GameEvent == 0)
                        {
                            tmpGo.instance = TileInstance;
                            gameObjects.AddToWorld(tmpGo);
                        }
                    }
                    catch (Exception ex3)
                    {
                        logger.LogCritical("Error when creating gameobject [{0}].{1}{2}", row["id"], Environment.NewLine, ex3.ToString());
                    }
                }
            }
            finally
            {
                if (enumerator2 is IDisposable)
                {
                    (enumerator2 as IDisposable).Dispose();
                }
            }
            MysqlQuery.Clear();
            characterDatabase.Query(string.Format("SELECT * FROM corpse WHERE map={0} AND instance={5} AND position_x BETWEEN '{1}' AND '{2}' AND position_y BETWEEN '{3}' AND '{4}';", TileMap, MinX, MaxX, MinY, MaxY, TileInstance), ref MysqlQuery);
            IEnumerator enumerator3 = default;
            try
            {
                enumerator3 = MysqlQuery.Rows.GetEnumerator();
                while (enumerator3.MoveNext())
                {
                    DataRow InfoRow = (DataRow)enumerator3.Current;
                    if (!worldState.WorldCorpseObjects.ContainsKey(Conversions.ToULong(InfoRow["guid"]) + MangosGlobalConstants.GUID_CORPSE))
                    {
                        try
                        {
                            var tmpCorpse = corpseObjectFactory.Create(Conversions.ToULong(InfoRow["guid"]), InfoRow);
                            tmpCorpse.instance = TileInstance;
                            tmpCorpse.AddToWorld();
                        }
                        catch (Exception ex7)
                        {
                            ProjectData.SetProjectError(ex7);
                            var ex2 = ex7;
                            logger.LogCritical("Error when creating corpse [{0}].{1}{2}", InfoRow["guid"], Environment.NewLine, ex2.ToString());
                            ProjectData.ClearProjectError();
                        }
                    }
                }
            }
            finally
            {
                if (enumerator3 is IDisposable)
                {
                    (enumerator3 as IDisposable).Dispose();
                }
            }
            try
            {
                worldState.WorldTransportsLock.EnterReadLock();
                foreach (var Transport in worldState.WorldTransports)
                {
                    try
                    {
                        if (Transport.Value.MapID == TileMap && Transport.Value.positionX >= MinX && Transport.Value.positionX <= MaxX && Transport.Value.positionY >= MinY && Transport.Value.positionY <= MaxY)
                        {
                            if (!maps.Maps[TileMap].Tiles[TileX, TileY].GameObjectsHere.Contains(Transport.Value.GUID))
                            {
                                maps.Maps[TileMap].Tiles[TileX, TileY].GameObjectsHere.Add(Transport.Value.GUID);
                            }
                            Transport.Value.NotifyEnter();
                        }
                    }
                    catch (Exception ex8)
                    {
                        ProjectData.SetProjectError(ex8);
                        var ex = ex8;
                        logger.LogCritical("Error when creating transport [{0}].{1}{2}", Transport.Key - MangosGlobalConstants.GUID_MO_TRANSPORT, Environment.NewLine, ex.ToString());
                        ProjectData.ClearProjectError();
                    }
                }
            }
            catch (Exception projectError)
            {
                ProjectData.SetProjectError(projectError);
                ProjectData.ClearProjectError();
            }
            finally
            {
                worldState.WorldTransportsLock.ExitReadLock();
            }
        }
    }

    public void UnloadSpawns(byte TileX, byte TileY, uint TileMap)
    {
        checked
        {
            var MinX = (32 - TileX) * MangosGlobalConstants.SIZE;
            var MaxX = (32 - (TileX + 1)) * MangosGlobalConstants.SIZE;
            var MinY = (32 - TileY) * MangosGlobalConstants.SIZE;
            var MaxY = (32 - (TileY + 1)) * MangosGlobalConstants.SIZE;
            if (MinX > MaxX)
            {
                var tmpSng2 = MinX;
                MinX = MaxX;
                MaxX = tmpSng2;
            }
            if (MinY > MaxY)
            {
                var tmpSng = MinY;
                MinY = MaxY;
                MaxY = tmpSng;
            }
            try
            {
                worldState.WorldCreaturesLock.EnterReadLock();
                foreach (var Creature in worldState.WorldCreatures)
                {
                    if (Creature.Value.MapID == TileMap && Creature.Value.SpawnX >= MinX && Creature.Value.SpawnX <= MaxX && Creature.Value.SpawnY >= MinY && Creature.Value.SpawnY <= MaxY)
                    {
                        Creature.Value.Destroy();
                    }
                }
            }
            catch (Exception ex2)
            {
                ProjectData.SetProjectError(ex2);
                var ex = ex2;
                logger.LogCritical(ex.ToString(), null);
                ProjectData.ClearProjectError();
            }
            finally
            {
                worldState.WorldCreaturesLock.ExitReadLock();
            }
            foreach (var Gameobject in worldState.WorldGameObjects)
            {
                if (Gameobject.Value.MapID == TileMap && Gameobject.Value.positionX >= MinX && Gameobject.Value.positionX <= MaxX && Gameobject.Value.positionY >= MinY && Gameobject.Value.positionY <= MaxY)
                {
                    gameObjects.Destroy(Gameobject.Value);
                }
            }
            foreach (var Corpseobject in worldState.WorldCorpseObjects)
            {
                if (Corpseobject.Value.MapID == TileMap && Corpseobject.Value.positionX >= MinX && Corpseobject.Value.positionX <= MaxX && Corpseobject.Value.positionY >= MinY && Corpseobject.Value.positionY <= MaxY)
                {
                    Corpseobject.Value.Destroy();
                }
            }
        }
    }

}
