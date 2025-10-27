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
using Mangos.World.Objects.Factories.Spells;
using Mangos.World.Player;
using Mangos.World.Spells;
using Microsoft.Extensions.Logging;
using static Mangos.World.Handlers.WS_Combat;
using static Mangos.World.Objects.WS_Base;

namespace Mangos.World.Objects.Factories.Combat;
public class AttackTimerFactory(ILogger<TAttackTimer> logger, WS_Combat combat, WS_Spells spells, SpellTargetsFactory spellTargetsFactory, CastSpellParametersFactory castSpellParametersFactory, ItemInfoFactory itemInfoFactory)
{
    public TAttackTimer Create(ref CharacterObject character)
    {
        return new TAttackTimer(logger, combat, spells, spellTargetsFactory, castSpellParametersFactory, itemInfoFactory, ref character);
    }

    public TAttackTimer Create(ref BaseObject victim, ref CharacterObject character)
    {
        return new TAttackTimer(logger, combat, spells, spellTargetsFactory, castSpellParametersFactory, itemInfoFactory, ref victim, ref character);
    }
}
