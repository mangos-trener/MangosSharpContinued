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

using Mangos.Configuration;
using Mangos.World.DataStores;
using Mangos.World.Handlers;
using Mangos.World.Loots;
using Mangos.World.Maps;
using Mangos.World.Network;
using Mangos.World.Services;
using Microsoft.Extensions.Logging;
using static Mangos.World.Objects.WS_Totems;

namespace Mangos.World.Objects.Factories;
public class TotemObjectFactory(
    ILogger<TotemObject> logger,
    WorldState worldState,
    MangosConfiguration configuration,
    WS_DBCDatabase database,
    WS_Maps maps,
    WS_Creatures creatures,
    WS_Combat combat,
    WS_Loot loot,
    IMapTileLoader mapTileLoader,
    WS_Network network,
    CreatureInfoFactory creatureInfoFactory)
{
    public TotemObject Create(int entry, float posX, float posY, float posZ, float orientation, int mapId, int duration = 0)
    {
        return new TotemObject(logger, worldState, configuration, database, maps, creatures, combat, loot, mapTileLoader, network, creatureInfoFactory, entry, posX, posY, posZ, orientation, mapId, duration);
    }
}
