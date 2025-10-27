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

using Mangos.World.Maps;
using Mangos.World.Objects.Factories.Maps;
using Microsoft.Extensions.Logging;
using System;

namespace Mangos.World.Services;
public class MapTileLoader(
    ILogger<MapTileLoader> logger,
    WS_Maps maps,
    Func<WS_Spawns> spawns,
    MapTileFactory mapTileFactory)
: IMapTileLoader
{
    public void LoadMap(byte x, byte y, uint mapId)
    {
        short i = -1;
        checked
        {
            do
            {
                short j = -1;
                do
                {
                    if ((short)unchecked(x + i) > -1 && (short)unchecked(x + i) < 64 && (short)unchecked(y + j) > -1 && (short)unchecked(y + j) < 64 && !maps.Maps[mapId].TileUsed[(short)unchecked(x + i), (short)unchecked(y + j)])
                    {
                        logger.LogInformation("Loading map [{2}: {0},{1}]...", (short)unchecked(x + i), (short)unchecked(y + j), mapId);
                        maps.Maps[mapId].TileUsed[(short)unchecked(x + i), (short)unchecked(y + j)] = true;

                        var spawnsInstance = spawns();
                        maps.Maps[mapId].Tiles[(short)unchecked(x + i), (short)unchecked(y + j)] = mapTileFactory.Create((byte)(short)unchecked(x + i), (byte)(short)unchecked(y + j), mapId, spawnsInstance);
                        spawnsInstance.LoadSpawns((byte)(short)unchecked(x + i), (byte)(short)unchecked(y + j), mapId, 0u);
                    }
                    j = (short)unchecked(j + 1);
                }
                while (j <= 1);
                i = (short)unchecked(i + 1);
            }
            while (i <= 1);
        }
    }

    public void UnloadMap(byte x, byte y, int mapId)
    {
        checked
        {
            if (maps.Maps[(uint)mapId].Tiles[x, y].PlayersHere.Count == 0)
            {
                logger.LogInformation("Unloading map [{2}: {0},{1}]...", x, y, mapId);
                maps.Maps[(uint)mapId].Tiles[x, y].Dispose();
                maps.Maps[(uint)mapId].Tiles[x, y] = null;
            }
        }
    }

}
