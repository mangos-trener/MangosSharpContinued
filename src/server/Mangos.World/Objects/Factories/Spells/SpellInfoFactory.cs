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
using Mangos.World.Handlers;
using Mangos.World.Maps;
using Mangos.World.Objects.Factories.Packets;
using Mangos.World.Spells;
using Microsoft.Extensions.Logging;
using static Mangos.World.Spells.WS_Spells;

namespace Mangos.World.Objects.Factories.Spells;
public class SpellInfoFactory(
    ILogger<SpellInfo> logger,
    WorldState worldState,
    WS_DBCDatabase database,
    CharacterDatabase characterDatabase,
    WorldDatabase worldDatabase,
    WS_Maps maps,
    WS_Spells spells,
    WS_Combat combat,
    ItemInfoFactory itemInfoFactory,
    UpdateClassFactory updateClassFactory)
{
    public SpellInfo Create()
    {
        return new SpellInfo(logger, worldState, database, characterDatabase, worldDatabase, maps, spells, combat, itemInfoFactory, updateClassFactory);
    }
}
