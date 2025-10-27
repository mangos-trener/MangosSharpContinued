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

using Mangos.Common.Legacy;
using Mangos.Common.Legacy.Databases;
using Mangos.Configuration;
using Mangos.World.DataStores;
using Mangos.World.Handlers;
using Mangos.World.Maps;
using Mangos.World.Network;
using Mangos.World.Objects.Factories.Combat;
using Mangos.World.Objects.Factories.Packets;
using Mangos.World.Objects.Factories.Spells;
using Mangos.World.Player;
using Mangos.World.Services;
using Microsoft.Extensions.Logging;
using static Mangos.World.Objects.WS_Base;

namespace Mangos.World.Objects.Factories;
public class CharacterObjectFactory(
    ILogger<BaseUnit> baseLogger,
    ILogger<CharacterObject> logger,
    ICluster cluster,
    MangosConfiguration configuration,
    WorldState worldState,
    WS_DBCDatabase database,
    CharacterDatabase characterDatabase,
    WS_Maps maps,
    WS_Player_Initializator playerInitializator,
    WS_Network network,
    WS_Combat combat,
    WS_Pets pets,
    WS_CharMovement charMovement,
    WS_Handlers_Misc misc,
    WS_PlayerHelper playerHelper,
    WS_Handlers_Instance instanceHandler,
    WS_GameObjects gameObjectsService,
    IMapTileLoader mapTileLoader,
    ICellUpdater cellUpdater,
    GameObjectFactory gameObjectFactory,
    NpcTextFactory npcTextFactory,
    UpdateClassFactory updateClassFactory,
    ItemObjectFactory itemObjectFactory,
    BaseActiveSpellFactory baseActiveSpellFactory,
    SpellTargetsFactory spellTargetsFactory,
    CastSpellParametersFactory castSpellParametersFactory,
    AttackTimerFactory attackTimerFactory,
    CorpseObjectFactory corpseObjectFactory)
{
    public CharacterObject Create()
    {
        var characterObject = new CharacterObject(
            baseLogger,
            logger,
            cluster,
            configuration,
            worldState,
            database,
            characterDatabase,
            maps,
            playerInitializator,
            network,
            combat,
            charMovement,
            misc,
            playerHelper,
            instanceHandler,
            gameObjectsService,
            mapTileLoader,
            cellUpdater,
            gameObjectFactory,
            npcTextFactory,
            updateClassFactory,
            itemObjectFactory,
            baseActiveSpellFactory,
            spellTargetsFactory,
            castSpellParametersFactory,
            attackTimerFactory,
            corpseObjectFactory);

        pets.LoadPet(ref characterObject);

        return characterObject;
    }

    public CharacterObject Create(ref WS_Network.ClientClass ClientVal, ulong GuidVal)
    {
        var characterObject = new CharacterObject(
            baseLogger,
            logger,
            cluster,
            configuration,
            worldState,
            database,
            characterDatabase,
            maps,
            playerInitializator,
            network,
            combat,
            charMovement,
            misc,
            playerHelper,
            instanceHandler,
            gameObjectsService,
            mapTileLoader,
            cellUpdater,
            gameObjectFactory,
            npcTextFactory,
            updateClassFactory,
            itemObjectFactory,
            baseActiveSpellFactory,
            spellTargetsFactory,
            castSpellParametersFactory,
            attackTimerFactory,
            corpseObjectFactory,
            ref ClientVal, GuidVal);

        pets.LoadPet(ref characterObject);

        return characterObject;
    }
}
