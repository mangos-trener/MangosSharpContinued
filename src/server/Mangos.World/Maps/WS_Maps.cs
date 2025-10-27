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

using Mangos.Common.Enums.Map;
using Mangos.Common.Globals;
using Mangos.Configuration;
using Mangos.DataStores;
using Mangos.World.Globals;
using Mangos.World.Network;
using Mangos.World.Objects;
using Mangos.World.Objects.Factories.Maps;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mangos.World.Maps;

public partial class WS_Maps
{
    private readonly ILogger<WS_Maps> logger;
    private readonly DataStoreProvider dataStoreProvider;
    private readonly MangosConfiguration _configuration;
    private readonly MapFactory mapFactory;
    public Dictionary<int, TArea> AreaTable;

    public int RESOLUTION_ZMAP;

    public Dictionary<uint, TMap> Maps;

    public string MapList;

    public WS_Maps(
        ILogger<WS_Maps> logger,
        DataStoreProvider dataStoreProvider,
        MangosConfiguration configuration,
        MapFactory mapFactory)
    {
        this.logger = logger;
        this.dataStoreProvider = dataStoreProvider;
        _configuration = configuration;
        this.mapFactory = mapFactory;
        AreaTable = new Dictionary<int, TArea>();
        RESOLUTION_ZMAP = 0;
        Maps = new Dictionary<uint, TMap>();
    }

    public int GetAreaIDByMapandParent(int mapId, int parentID)
    {
        foreach (var thisArea in AreaTable)
        {
            var thisMap = thisArea.Value.mapId;
            var thisParent = thisArea.Value.Zone;
            if (thisMap == mapId && thisParent == parentID)
            {
                return thisArea.Key;
            }
        }
        return -999;
    }

    public async Task InitializeMapsAsync()
    {
        var e = _configuration.World.Maps.GetEnumerator();
        if (e.MoveNext())
        {
            MapList = Conversions.ToString(e.Current);
            while (e.MoveNext())
            {
                MapList = Conversions.ToString(Operators.AddObject(MapList, Operators.ConcatenateObject(", ", e.Current)));
            }
        }
        foreach (var map2 in _configuration.World.Maps)
        {
            var id = Conversions.ToUInteger(map2);
            var map = mapFactory.Create(checked((int)id), await dataStoreProvider.GetDataStoreAsync("Map.dbc"));
        }

        logger.LogInformation("Initalizing: {0} Maps initialized.", Maps.Count);
    }

    public async ValueTask<TMap> LoadMap(int id)
    {
        if (Maps.ContainsKey((uint)id))
            return Maps[(uint)id];

        var map = mapFactory.Create(id, await dataStoreProvider.GetDataStoreAsync("Map.dbc"));
        Maps[(uint)id] = map;

        return map;
    }

    public float ValidateMapCoord(float coord)
    {
        if (coord > 32f * MangosGlobalConstants.SIZE)
        {
            coord = 32f * MangosGlobalConstants.SIZE;
        }
        else if (coord < -32f * MangosGlobalConstants.SIZE)
        {
            coord = -32f * MangosGlobalConstants.SIZE;
        }
        return coord;
    }

    public void GetMapTile(float x, float y, ref byte MapTileX, ref byte MapTileY)
    {
        checked
        {
            MapTileX = (byte)(32f - (ValidateMapCoord(x) / MangosGlobalConstants.SIZE));
            MapTileY = (byte)(32f - (ValidateMapCoord(y) / MangosGlobalConstants.SIZE));
        }
    }

    public byte GetMapTileX(float x)
    {
        return checked((byte)(32f - (ValidateMapCoord(x) / MangosGlobalConstants.SIZE)));
    }

    public byte GetMapTileY(float y)
    {
        return checked((byte)(32f - (ValidateMapCoord(y) / MangosGlobalConstants.SIZE)));
    }

    public byte GetSubMapTileX(float x)
    {
        return checked((byte)(RESOLUTION_ZMAP * (32f - (ValidateMapCoord(x) / MangosGlobalConstants.SIZE) - Conversion.Fix(32f - (ValidateMapCoord(x) / MangosGlobalConstants.SIZE)))));
    }

    public byte GetSubMapTileY(float y)
    {
        return checked((byte)(RESOLUTION_ZMAP * (32f - (ValidateMapCoord(y) / MangosGlobalConstants.SIZE) - Conversion.Fix(32f - (ValidateMapCoord(y) / MangosGlobalConstants.SIZE)))));
    }

    public float GetZCoord(float x, float y, uint Map)
    {
        checked
        {
            try
            {
                x = ValidateMapCoord(x);
                y = ValidateMapCoord(y);
                var MapTileX = (byte)(32f - (x / MangosGlobalConstants.SIZE));
                var MapTileY = (byte)(32f - (y / MangosGlobalConstants.SIZE));
                var MapTile_LocalX = (byte)Math.Round(RESOLUTION_ZMAP * (32f - (x / MangosGlobalConstants.SIZE) - MapTileX));
                var MapTile_LocalY = (byte)Math.Round(RESOLUTION_ZMAP * (32f - (y / MangosGlobalConstants.SIZE) - MapTileY));
                float xNormalized;
                float yNormalized;
                unchecked
                {
                    xNormalized = (RESOLUTION_ZMAP * (32f - (x / MangosGlobalConstants.SIZE) - MapTileX)) - MapTile_LocalX;
                    yNormalized = (RESOLUTION_ZMAP * (32f - (y / MangosGlobalConstants.SIZE) - MapTileY)) - MapTile_LocalY;
                    if (Maps[Map].Tiles[MapTileX, MapTileY] == null)
                    {
                        return 0f;
                    }
                }
                try
                {
                    var topHeight = Globals.Functions.MathLerp(GetHeight(Map, MapTileX, MapTileY, MapTile_LocalX, MapTile_LocalY), GetHeight(Map, MapTileX, MapTileY, (byte)(MapTile_LocalX + 1), MapTile_LocalY), xNormalized);
                    var bottomHeight = Globals.Functions.MathLerp(GetHeight(Map, MapTileX, MapTileY, MapTile_LocalX, (byte)(MapTile_LocalY + 1)), GetHeight(Map, MapTileX, MapTileY, (byte)(MapTile_LocalX + 1), (byte)(MapTile_LocalY + 1)), xNormalized);
                    return Globals.Functions.MathLerp(topHeight, bottomHeight, yNormalized);
                }
                catch (Exception ex)
                {
                    var GetZCoord = Maps[Map].Tiles[MapTileX, MapTileY].ZCoord[MapTile_LocalX, MapTile_LocalY];
                    logger.LogWarning("GetHeight threw an Exception : GetZCoord {0}, {1}", GetZCoord, ex);
                    return GetZCoord;
                }
            }
            catch (Exception ex2)
            {
                var GetZCoord = 0f;
                logger.LogWarning("GetZCoord threw an Exception : Coord X {0} Coord Y {1} Coord Z {2}, {3}", x, y, GetZCoord, ex2);
                return GetZCoord;
            }
        }
    }

    public float GetWaterLevel(float x, float y, int Map)
    {
        x = ValidateMapCoord(x);
        y = ValidateMapCoord(y);
        checked
        {
            var MapTileX = (byte)(32f - (x / MangosGlobalConstants.SIZE));
            var MapTileY = (byte)(32f - (y / MangosGlobalConstants.SIZE));
            var MapTile_LocalX = (byte)Math.Round(MangosGlobalConstants.RESOLUTION_WATER * (32f - (x / MangosGlobalConstants.SIZE) - MapTileX));
            var MapTile_LocalY = (byte)Math.Round(MangosGlobalConstants.RESOLUTION_WATER * (32f - (y / MangosGlobalConstants.SIZE) - MapTileY));
            return Maps[(uint)Map].Tiles[MapTileX, MapTileY] == null
                ? 0f
                : Maps[(uint)Map].Tiles[MapTileX, MapTileY].WaterLevel[MapTile_LocalX, MapTile_LocalY];
        }
    }

    public byte GetTerrainType(float x, float y, int Map)
    {
        x = ValidateMapCoord(x);
        y = ValidateMapCoord(y);
        checked
        {
            var MapTileX = (byte)(32f - (x / MangosGlobalConstants.SIZE));
            var MapTileY = (byte)(32f - (y / MangosGlobalConstants.SIZE));
            var MapTile_LocalX = (byte)Math.Round(MangosGlobalConstants.RESOLUTION_TERRAIN * (32f - (x / MangosGlobalConstants.SIZE) - MapTileX));
            var MapTile_LocalY = (byte)Math.Round(MangosGlobalConstants.RESOLUTION_TERRAIN * (32f - (y / MangosGlobalConstants.SIZE) - MapTileY));
            return (byte)(Maps[(uint)Map].Tiles[MapTileX, MapTileY] == null
                ? 0
                : Maps[(uint)Map].Tiles[MapTileX, MapTileY].AreaTerrain[MapTile_LocalX, MapTile_LocalY]);
        }
    }

    public int GetAreaFlag(float x, float y, int Map)
    {
        x = ValidateMapCoord(x);
        y = ValidateMapCoord(y);
        checked
        {
            var MapTileX = (byte)(32f - (x / MangosGlobalConstants.SIZE));
            var MapTileY = (byte)(32f - (y / MangosGlobalConstants.SIZE));
            var MapTile_LocalX = (byte)Math.Round(MangosGlobalConstants.RESOLUTION_FLAGS * (32f - (x / MangosGlobalConstants.SIZE) - MapTileX));
            var MapTile_LocalY = (byte)Math.Round(MangosGlobalConstants.RESOLUTION_FLAGS * (32f - (y / MangosGlobalConstants.SIZE) - MapTileY));
            return Maps[(uint)Map].Tiles[MapTileX, MapTileY] == null
                ? 0
                : Maps[(uint)Map].Tiles[MapTileX, MapTileY].AreaFlag[MapTile_LocalX, MapTile_LocalY];
        }
    }

    public bool IsOutsideOfMap(ref WS_Base.BaseObject objCharacter)
    {
        return false;
    }

    public float GetZCoord(float x, float y, float z, uint Map)
    {
        checked
        {
            try
            {
                x = ValidateMapCoord(x);
                y = ValidateMapCoord(y);
                z = ValidateMapCoord(z);
                var MapTileX = (byte)(32f - (x / MangosGlobalConstants.SIZE));
                var MapTileY = (byte)(32f - (y / MangosGlobalConstants.SIZE));
                var MapTile_LocalX = (byte)Math.Round(RESOLUTION_ZMAP * (32f - (x / MangosGlobalConstants.SIZE) - MapTileX));
                var MapTile_LocalY = (byte)Math.Round(RESOLUTION_ZMAP * (32f - (y / MangosGlobalConstants.SIZE) - MapTileY));
                float xNormalized;
                float yNormalized;
                unchecked
                {
                    xNormalized = (RESOLUTION_ZMAP * (32f - (x / MangosGlobalConstants.SIZE) - MapTileX)) - MapTile_LocalX;
                    yNormalized = (RESOLUTION_ZMAP * (32f - (y / MangosGlobalConstants.SIZE) - MapTileY)) - MapTile_LocalY;
                    if (Maps[Map].Tiles[MapTileX, MapTileY] == null)
                    {
                        var VMapHeight2 = GetVMapHeight(Map, x, y, z + 5f);
                        return VMapHeight2 != MangosGlobalConstants.VMAP_INVALID_HEIGHT_VALUE ? VMapHeight2 : 0f;
                    }
                    if (Math.Abs(Maps[Map].Tiles[MapTileX, MapTileY].ZCoord[MapTile_LocalX, MapTile_LocalY] - z) >= 2f)
                    {
                        var VMapHeight = GetVMapHeight(Map, x, y, z + 5f);
                        if (VMapHeight != MangosGlobalConstants.VMAP_INVALID_HEIGHT_VALUE)
                        {
                            return VMapHeight;
                        }
                    }
                }
                try
                {
                    var topHeight = Globals.Functions.MathLerp(GetHeight(Map, MapTileX, MapTileY, MapTile_LocalX, MapTile_LocalY), GetHeight(Map, MapTileX, MapTileY, MapTile_LocalX, MapTile_LocalY), xNormalized);
                    var bottomHeight = Globals.Functions.MathLerp(GetHeight(Map, MapTileX, MapTileY, MapTile_LocalX, MapTile_LocalY), GetHeight(Map, MapTileX, MapTileY, MapTile_LocalX, MapTile_LocalY), xNormalized);
                    return Globals.Functions.MathLerp(topHeight, bottomHeight, yNormalized);
                }
                catch (Exception ex)
                {
                    var GetZCoord = Maps[Map].Tiles[MapTileX, MapTileY].ZCoord[MapTile_LocalX, MapTile_LocalY];
                    logger.LogWarning("GetZCoord threw an Exception : Coord X {0} Coord Y {1} Coord Z {2}, {3}", x, y, GetZCoord, ex);
                    return GetZCoord;
                }
            }
            catch (Exception ex2)
            {
                logger.LogError(ex2.ToString());
                var GetZCoord = z;
                return GetZCoord;
            }
        }
    }

    private float GetHeight(uint Map, byte MapTileX, byte MapTileY, byte MapTileLocalX, byte MapTileLocalY)
    {
        checked
        {
            if (MapTileLocalX > RESOLUTION_ZMAP)
            {
                MapTileX = (byte)(MapTileX + 1);
                MapTileLocalX = (byte)(MapTileLocalX - (RESOLUTION_ZMAP + 1));
            }
            else if (MapTileLocalX < 0)
            {
                MapTileX = (byte)(MapTileX - 1);
                MapTileLocalX = (byte)((short)unchecked(-MapTileLocalX) - 1);
            }
            if (MapTileLocalY > RESOLUTION_ZMAP)
            {
                MapTileY = (byte)(MapTileY + 1);
                MapTileLocalY = (byte)(MapTileLocalY - (RESOLUTION_ZMAP + 1));
            }
            else if (MapTileLocalY < 0)
            {
                MapTileY = (byte)(MapTileY - 1);
                MapTileLocalY = (byte)((short)unchecked(-MapTileLocalY) - 1);
            }
            return Maps[Map].Tiles[MapTileX, MapTileY].ZCoord[MapTileLocalX, MapTileLocalY];
        }
    }

    public bool IsInLineOfSight(ref WS_Base.BaseObject obj, ref WS_Base.BaseObject obj2)
    {
        return IsInLineOfSight(obj.MapID, obj.positionX, obj.positionY, obj.positionZ + 2f, obj2.positionX, obj2.positionY, obj2.positionZ + 2f);
    }

    public bool IsInLineOfSight(ref WS_Base.BaseObject obj, float x2, float y2, float z2)
    {
        x2 = ValidateMapCoord(x2);
        y2 = ValidateMapCoord(y2);
        z2 = ValidateMapCoord(z2);
        return IsInLineOfSight(obj.MapID, obj.positionX, obj.positionY, obj.positionZ + 2f, x2, y2, z2);
    }

    public bool IsInLineOfSight(uint MapID, float x1, float y1, float z1, float x2, float y2, float z2)
    {
        x1 = ValidateMapCoord(x1);
        y1 = ValidateMapCoord(y1);
        z1 = ValidateMapCoord(z1);
        x2 = ValidateMapCoord(x2);
        y2 = ValidateMapCoord(y2);
        z2 = ValidateMapCoord(z2);
        return true;
    }

    public float GetVMapHeight(uint MapID, float x, float y, float z)
    {
        x = ValidateMapCoord(x);
        y = ValidateMapCoord(y);
        z = ValidateMapCoord(z);
        return MangosGlobalConstants.VMAP_INVALID_HEIGHT_VALUE;
    }

    public bool GetObjectHitPos(ref WS_Base.BaseObject obj, ref WS_Base.BaseObject obj2, ref float rx, ref float ry, ref float rz, float pModifyDist)
    {
        rx = ValidateMapCoord(rx);
        ry = ValidateMapCoord(ry);
        rz = ValidateMapCoord(rz);
        return GetObjectHitPos(obj.MapID, obj.positionX, obj.positionY, obj.positionZ + 2f, obj2.positionX, obj2.positionY, obj2.positionZ + 2f, ref rx, ref ry, ref rz, pModifyDist);
    }

    public bool GetObjectHitPos(ref WS_Base.BaseObject obj, float x2, float y2, float z2, ref float rx, ref float ry, ref float rz, float pModifyDist)
    {
        rx = ValidateMapCoord(rx);
        ry = ValidateMapCoord(ry);
        rz = ValidateMapCoord(rz);
        x2 = ValidateMapCoord(x2);
        y2 = ValidateMapCoord(y2);
        z2 = ValidateMapCoord(z2);
        return GetObjectHitPos(obj.MapID, obj.positionX, obj.positionY, obj.positionZ + 2f, x2, y2, z2, ref rx, ref ry, ref rz, pModifyDist);
    }

    public bool GetObjectHitPos(uint MapID, float x1, float y1, float z1, float x2, float y2, float z2, ref float rx, ref float ry, ref float rz, float pModifyDist)
    {
        x1 = ValidateMapCoord(x1);
        y1 = ValidateMapCoord(y1);
        z1 = ValidateMapCoord(z1);
        x2 = ValidateMapCoord(x2);
        y2 = ValidateMapCoord(y2);
        z2 = ValidateMapCoord(z2);
        return false;
    }

    public void SendTransferAborted(ref WS_Network.ClientClass client, int Map, TransferAbortReason Reason)
    {
        logger.LogDebug("[{0}:{1}] SMSG_TRANSFER_ABORTED [{2}:{3}]", client.IP, client.Port, Map, Reason);
        Packets.PacketClass p = new(Opcodes.SMSG_TRANSFER_ABORTED);
        try
        {
            p.AddInt32(Map);
            p.AddInt16((short)Reason);
            client.Send(ref p);
        }
        finally
        {
            p.Dispose();
        }
    }
}
