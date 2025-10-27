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
using Mangos.World.Objects.Factories.Quests;
using Mangos.World.Spells;
using Microsoft.Extensions.Logging;
using static Mangos.World.Objects.WS_NPCs;

namespace Mangos.World.Objects.Factories;
public class DefaultTalkFactory(ILogger<TDefaultTalk> logger, WorldState worldState, WorldDatabase worldDatabase, WS_Handlers_Taxi taxi, WS_Spells spells, IQuestInfoFactory questInfoFactory, ItemInfoFactory itemInfoFactory)
{
    public TDefaultTalk Create()
    {
        return new TDefaultTalk(logger, worldState, worldDatabase, taxi, spells, questInfoFactory, itemInfoFactory);
    }
}
