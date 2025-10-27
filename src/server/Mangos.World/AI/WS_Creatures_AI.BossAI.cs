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
using Mangos.World.Objects;
using Mangos.World.Player;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace Mangos.World.AI;

public partial class WS_Creatures_AI
{
    public class BossAI : DefaultAI
    {
        public BossAI(ILogger<BossAI> logger, WorldState worldState, WS_Maps maps, WS_Loot loot, WS_Creatures creatures, WS_Combat combat, ref WS_Creatures.CreatureObject Creature)
            : base(logger, worldState, ref Creature, maps, loot, creatures, combat)
        {
        }

        public override void OnEnterCombat()
        {
            base.OnEnterCombat();
            foreach (var Unit in aiHateTable)
            {
                if (Unit.Key is not CharacterObject)
                {
                    continue;
                }
                CharacterObject characterObject = (CharacterObject)Unit.Key;
                if (characterObject.IsInGroup)
                {
                    var array = characterObject.Group.LocalMembers.ToArray();
                    foreach (var member in array)
                    {
                        if (worldState.Characters.ContainsKey(member) && worldState.Characters[member].MapID == characterObject.MapID && worldState.Characters[member].instance == characterObject.instance)
                        {
                            aiHateTable.Add(worldState.Characters[member], 0);
                        }
                    }
                    break;
                }
            }
        }

        public override void DoThink()
        {
            base.DoThink();
            new Thread(OnThink)
            {
                Name = "Boss Thinking"
            }.Start();
        }

        public virtual void OnThink()
        {
        }
    }
}
