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
using Mangos.World.DataStores;
using Mangos.World.Objects.Factories.Loot;
using Mangos.World.Objects.Factories.Packets;
using Mangos.World.Player;
using Microsoft.Extensions.Logging;
using System;

namespace Mangos.World.Objects.Factories;
public class ItemObjectFactory(
    ILogger<ItemObject> logger,
    WorldState worldState,
    CharacterDatabase characterDatabase,
    WorldDatabase worldDatabase,
    WS_DBCDatabase database,
    ItemInfoFactory itemInfoFactory,
    Func<LootObjectFactory> lootObjectFactory,
    UpdateClassFactory updateClassFactory)
{
    public ItemObject Create()
    {
        return new ItemObject(logger, worldState, database, characterDatabase, worldDatabase, itemInfoFactory, lootObjectFactory, updateClassFactory);
    }

    public ItemObject Create(int id, ulong ownerGuid)
    {
        return new ItemObject(logger, worldState, database, characterDatabase, worldDatabase, itemInfoFactory, lootObjectFactory, updateClassFactory, id, ownerGuid);
    }

    public ItemObject Create(int id, ulong ownerGuid, int stackCount)
    {
        var model = Create(id, ownerGuid);
        model.StackCount = stackCount;

        return model;
    }

    public ItemObject Create(ulong guid, CharacterObject owner = null, bool equipped = false)
    {
        return new ItemObject(logger, worldState, database, characterDatabase, worldDatabase, itemInfoFactory, lootObjectFactory, updateClassFactory, guid, owner, equipped);
    }
}
