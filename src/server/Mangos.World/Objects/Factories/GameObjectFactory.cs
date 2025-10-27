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

using Mangos.Common.Legacy.Databases;
using Mangos.World.Handlers;
using Mangos.World.Loots;
using Mangos.World.Maps;
using Mangos.World.Objects.Factories.Loot;
using Mangos.World.Objects.Factories.Packets;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Mangos.World.Objects.Factories;
public class GameObjectFactory(
    ILogger<GameObject> logger,
    WorldState worldState,
    WorldDatabase worldDatabase,
    WS_Maps maps,
    WS_Loot loot,
    WS_Combat combat,
    GameObjectInfoFactory gameObjectInfoFactory,
    LootObjectFactory lootObjectFactory,
    UpdateClassFactory updateClassFactory)
{
    public GameObject Create(int id, uint mapId, float posX, float posY, float posZ, float rotation, ulong owner = 0uL)
    {
        return new GameObject(logger, worldState, worldDatabase, maps, loot, combat, gameObjectInfoFactory, lootObjectFactory, updateClassFactory, id, mapId, posX, posY, posZ, rotation, owner);
    }

    public GameObject Create(ulong guid, DataRow dataRow = null)
    {
        return new GameObject(logger, worldState, worldDatabase, maps, loot, combat, gameObjectInfoFactory, lootObjectFactory, updateClassFactory, guid, dataRow);
    }
}
