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

using Mangos.Configuration;
using Mangos.World.DataStores;
using Mangos.World.Handlers;
using Mangos.World.Loots;
using Mangos.World.Maps;
using Mangos.World.Network;
using Mangos.World.Objects.Factories.CreaturesAi;
using Mangos.World.Objects.Factories.Loot;
using Mangos.World.Objects.Factories.Packets;
using Mangos.World.Objects.Factories.Spells;
using Mangos.World.Scripts;
using Mangos.World.Services;
using Microsoft.Extensions.Logging;
using System.Data;
using static Mangos.World.Objects.WS_Creatures;

namespace Mangos.World.Objects.Factories;
public class CreatureObjectFactory(
    ILogger<CreatureObject> logger,
    IScriptExecutor scriptExecutor,
    WorldState worldState,
    MangosConfiguration configuration,
    WS_DBCDatabase database,
    WS_Maps maps,
    WS_Creatures creatures,
    WS_Combat combat,
    WS_Loot loot,
    WS_Network network,
    IMapTileLoader tileLoader,
    LootObjectFactory lootObjectFactory,
    CreatureInfoFactory creatureInfoFactory,
    BaseActiveSpellFactory baseActiveSpellFactory,
    SpellTargetsFactory spellTargetsFactory,
    CastSpellParametersFactory castSpellParametersFactory,
    UpdateClassFactory updateClassFactory,
    CritterAiFactory critterAiFactory,
    DefaultAiFactory defaultAiFactory,
    GuardAiFactory guardAiFactory,
    GuardWaypointAiFactory guardWaypointAiFactory,
    PetAiFactory petAiFactory,
    StandStillAiFactory standStillAiFactory,
    WaypointAiFactory waypointAiFactory)
{
    public CreatureObject Create(ulong guid, DataRow dataRow = null)
    {
        return new CreatureObject(
            logger,
            scriptExecutor,
            worldState,
            configuration,
            database,
            maps,
            creatures,
            combat,
            loot,
            network,
            tileLoader,
            lootObjectFactory,
            creatureInfoFactory,
            baseActiveSpellFactory,
            spellTargetsFactory,
            castSpellParametersFactory,
            updateClassFactory,
            critterAiFactory,
            defaultAiFactory,
            guardAiFactory,
            guardWaypointAiFactory,
            petAiFactory,
            standStillAiFactory,
            waypointAiFactory,
            guid,
            dataRow);
    }

    public CreatureObject Create(ulong guid, int id)
    {
        return new CreatureObject(
            logger,
            scriptExecutor,
            worldState,
            configuration,
            database,
            maps,
            creatures,
            combat,
            loot,
            network,
            tileLoader,
            lootObjectFactory,
            creatureInfoFactory,
            baseActiveSpellFactory,
            spellTargetsFactory,
            castSpellParametersFactory,
            updateClassFactory,
            critterAiFactory,
            defaultAiFactory,
            guardAiFactory,
            guardWaypointAiFactory,
            petAiFactory,
            standStillAiFactory,
            waypointAiFactory,
            guid,
            id);
    }

    public CreatureObject Create(int id,
            float posX,
            float posY,
            float posZ,
            float orientation_,
            int map,
            int duration = 0)
    {
        return new CreatureObject(
            logger,
            scriptExecutor,
            worldState,
            configuration,
            database,
            maps,
            creatures,
            combat,
            loot,
            network,
            tileLoader,
            lootObjectFactory,
            creatureInfoFactory,
            baseActiveSpellFactory,
            spellTargetsFactory,
            castSpellParametersFactory,
            updateClassFactory,
            critterAiFactory,
            defaultAiFactory,
            guardAiFactory,
            guardWaypointAiFactory,
            petAiFactory,
            standStillAiFactory,
            waypointAiFactory,
            id,
            posX,
            posY,
            posZ,
            orientation_,
            map);
    }
}
