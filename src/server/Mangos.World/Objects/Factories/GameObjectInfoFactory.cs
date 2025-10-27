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
using Microsoft.Extensions.Logging;
using static Mangos.World.Objects.WS_GameObjects;

namespace Mangos.World.Objects.Factories;
public class GameObjectInfoFactory(ILogger<GameObjectInfo> logger, WorldState worldState, WorldDatabase worldDatabase, WS_Maps maps)
{
    private readonly ILogger<GameObjectInfo> logger = logger;
    private readonly WorldState worldState = worldState;
    private readonly WorldDatabase worldDatabase = worldDatabase;
    private readonly WS_Maps maps = maps;

    public GameObjectInfo Create(int id)
    {
        return new GameObjectInfo(logger, worldState, worldDatabase, maps, id);
    }
}
