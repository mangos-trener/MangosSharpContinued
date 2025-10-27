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
using Microsoft.Extensions.Logging;
using static Mangos.World.Maps.WS_Maps;

namespace Mangos.World.Objects.Factories.Maps;
public class MapTileFactory(ILogger<TMapTile> logger, WS_Maps maps)
{
    // now accepts WS_Spawns at Create time to avoid requiring WS_Spawns in ctor
    public TMapTile Create(byte x, byte y, uint mapId, WS_Spawns spawns)
    {
        return new TMapTile(logger, maps, spawns, x, y, mapId);
    }
}
