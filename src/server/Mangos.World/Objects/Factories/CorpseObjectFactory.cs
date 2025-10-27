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
using Mangos.World.Maps;
using Mangos.World.Objects.Factories.Packets;
using Mangos.World.Player;
using Mangos.World.Services;
using Microsoft.Extensions.Logging;
using System.Data;
using static Mangos.World.Objects.WS_Corpses;

namespace Mangos.World.Objects.Factories;
public class CorpseObjectFactory(ILogger<CorpseObject> logger, WorldState worldState, CharacterDatabase characterDatabase, WS_Maps maps, IMapTileLoader tileLoader, UpdateClassFactory updateClassFactory)
{
    public CorpseObject Create(ref CharacterObject characterObject)
    {
        return new CorpseObject(logger, worldState, characterDatabase, maps, tileLoader, updateClassFactory, ref characterObject);
    }

    public CorpseObject Create(ulong guid, DataRow dataRow)
    {
        return new CorpseObject(logger, worldState, characterDatabase, maps, tileLoader, updateClassFactory, guid, dataRow);
    }
}
