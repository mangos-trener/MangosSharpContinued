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
using Mangos.DataStores;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System;
using System.IO;

namespace Mangos.World.Maps;

public partial class WS_Maps
{
    public class TMap : IDisposable
    {
        public int ID;

        public MapTypes Type;

        public string Name;

        public bool[,] TileUsed;

        public TMapTile[,] Tiles;

        private bool _disposedValue;
        private readonly ILogger<TMap> logger;

        public bool IsDungeon => Type is MapTypes.MAP_INSTANCE or MapTypes.MAP_RAID;

        public bool IsRaid => Type == MapTypes.MAP_RAID;

        public bool IsBattleGround => Type == MapTypes.MAP_BATTLEGROUND;

        public int ResetTime
        {
            get
            {
                checked
                {
                    switch (Type)
                    {
                        case MapTypes.MAP_BATTLEGROUND:
                            return MangosGlobalConstants.DEFAULT_BATTLEFIELD_EXPIRE_TIME;

                        case MapTypes.MAP_INSTANCE:
                        case MapTypes.MAP_RAID:
                            switch (ID)
                            {
                                case 249:
                                    return (int)Math.Round(Globals.Functions.GetNextDate(5, 3).Subtract(DateAndTime.Now).TotalSeconds);

                                case 309:
                                case 509:
                                    return (int)Math.Round(Globals.Functions.GetNextDate(3, 3).Subtract(DateAndTime.Now).TotalSeconds);

                                case 409:
                                case 469:
                                case 531:
                                case 533:
                                    return (int)Math.Round(Globals.Functions.GetNextDay(DayOfWeek.Tuesday, 3).Subtract(DateAndTime.Now).TotalSeconds);
                            }
                            break;
                        default:
                            break;
                    }
                    return MangosGlobalConstants.DEFAULT_INSTANCE_EXPIRE_TIME;
                }
            }
        }

        public TMap(ILogger<TMap> logger, int mapId, DataStore mapDataStore)
        {
            this.logger = logger;

            ID = mapId;
            Type = MapTypes.MAP_COMMON;
            Name = "";
            TileUsed = new bool[64, 64];
            Tiles = new TMapTile[64, 64];

            InitTiles();

            try
            {
                for (var i = 0; i < mapDataStore.Rows; i++)
                {
                    if (mapDataStore.ReadInt(i, 0) == mapId)
                    {
                        Type = (MapTypes)mapDataStore.ReadInt(i, 2);
                        Name = mapDataStore.ReadString(i, 4);
                        break;
                    }
                }

                logger.LogInformation("DBC: Map {MapId} initialized.", mapId);
            }
            catch (DirectoryNotFoundException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("DBC File : Map missing.");
                Console.ResetColor();
            }
        }

        private void InitTiles()
        {
            for (var x = 0; x < 64; x++)
                for (var y = 0; y < 64; y++)
                    TileUsed[x, y] = false;
        }

        protected virtual void Dispose(bool disposing)
        {
            checked
            {
                if (!_disposedValue)
                {
                    for (var i = 0; i < 64; i++)
                        for (var j = 0; j < 64; j++)
                            Tiles[i, j]?.Dispose();
                }
                _disposedValue = true;
            }
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
    }
}
