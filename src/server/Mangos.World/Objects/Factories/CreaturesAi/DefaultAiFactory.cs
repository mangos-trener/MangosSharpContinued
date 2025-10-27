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

using Mangos.World.Handlers;
using Mangos.World.Loots;
using Mangos.World.Maps;
using Microsoft.Extensions.Logging;
using static Mangos.World.AI.WS_Creatures_AI;

namespace Mangos.World.Objects.Factories.CreaturesAi;
public class DefaultAiFactory(ILogger<DefaultAI> logger, WorldState worldState, WS_Maps maps, WS_Loot loot, WS_Creatures creatures, WS_Combat combat)
{
    public DefaultAI Create(ref WS_Creatures.CreatureObject creature)
    {
        return new DefaultAI(logger, worldState, ref creature, maps, loot, creatures, combat);
    }
}
