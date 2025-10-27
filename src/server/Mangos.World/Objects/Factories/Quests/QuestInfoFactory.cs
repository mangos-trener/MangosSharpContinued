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
using Mangos.World.Quests;
using Microsoft.Extensions.Logging;

namespace Mangos.World.Objects.Factories.Quests;
public class QuestInfoFactory(
        ILogger<WS_QuestInfo> logger,
        WorldState worldState,
        WorldDatabase worldDatabase)
    : IQuestInfoFactory
{
    public WS_QuestInfo Create(int questId)
    {
        return new WS_QuestInfo(logger, worldState, worldDatabase, this, questId);
    }
}
