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
using Mangos.World.Objects.Factories.Spells;
using Mangos.World.Player;
using Mangos.World.Quests;
using Microsoft.Extensions.Logging;

namespace Mangos.World.Objects.Factories.Quests;
public class QuestsFactory(ILogger<WS_Quests> logger,
        WorldState worldState,
        CharacterDatabase characterDatabase,
        WorldDatabase worldDatabase,
        WS_Player_Initializator playerInitializator,
        ItemInfoFactory itemInfoFactory,
        ItemObjectFactory itemObjectFactory,
        SpellTargetsFactory spellTargetsFactory,
        CastSpellParametersFactory castSpellParametersFactory,
        BaseQuestFactory baseQuestFactory,
        IQuestInfoFactory questInfoFactory)
{
    public WS_Quests Create()
    {
        return new WS_Quests(logger, worldState, characterDatabase, worldDatabase, playerInitializator, itemInfoFactory, itemObjectFactory, spellTargetsFactory, castSpellParametersFactory, baseQuestFactory, questInfoFactory);
    }
}
