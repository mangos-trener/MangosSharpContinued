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

using Mangos.Common.Globals;
using Mangos.World.Handlers;
using Mangos.World.Maps;
using Mangos.World.Network;
using Mangos.World.Objects;
using Mangos.World.Player;
using Mangos.World.Quests;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Mangos.World;
public class WorldState
{
    public WorldState()
    {
        ConnectedClients = new Dictionary<uint, WS_Network.ClientClass>();
        Characters = new Dictionary<ulong, CharacterObject>();
        CharactersLock = new ReaderWriterLockSlim();
        CreatureQuestStarters = new Dictionary<int, List<int>>();
        CreatureQuestFinishers = new Dictionary<int, List<int>>();
        GameobjectQuestStarters = new Dictionary<int, List<int>>();
        GameobjectQuestFinishers = new Dictionary<int, List<int>>();
        WorldCreaturesLock = new ReaderWriterLockSlim();
        WorldCreatures = new Dictionary<ulong, WS_Creatures.CreatureObject>();
        WorldCreatureKeys = new ArrayList();
        WorldGameObjects = new Dictionary<ulong, GameObject>();
        WorldCorpseObjects = new Dictionary<ulong, WS_Corpses.CorpseObject>();
        WorldDynamicObjectsLock = new ReaderWriterLockSlim();
        WorldDynamicObjects = new Dictionary<ulong, WS_DynamicObjects.DynamicObject>();
        WorldTransportsLock = new ReaderWriterLockSlim();
        WorldTransports = new Dictionary<ulong, WS_Transports.TransportObject>();
        WorldItems = new Dictionary<ulong, ItemObject>();
        ItemDatabase = new Dictionary<int, WS_Items.ItemInfo>();
        CreaturesDatabase = new Dictionary<int, CreatureInfo>();
        GameObjectsDatabase = new Dictionary<int, GameObjectInfo>();

        ItemGuidCounter = MangosGlobalConstants.GUID_ITEM;
        CreatureGuidCounter = MangosGlobalConstants.GUID_UNIT;
        GameObjectsGuidCounter = MangosGlobalConstants.GUID_GAMEOBJECT;
        CorpseGuidCounter = MangosGlobalConstants.GUID_CORPSE;
        DynamicObjectsGuidCounter = MangosGlobalConstants.GUID_DYNAMICOBJECT;
        TransportGuidCounter = MangosGlobalConstants.GUID_MO_TRANSPORT;
    }

    public Dictionary<uint, WS_Network.ClientClass> ConnectedClients { get; init; }
    public Dictionary<ulong, CharManagementHandler> CharacterManagementHandler { get; init; }
    public Dictionary<ulong, WS_CharMovement> CharMovementService { get; init; }
    public Dictionary<ulong, WS_Combat> CombatService { get; init; }
    public Dictionary<ulong, WS_Handlers_Battleground> BattlegroundHandler { get; init; }
    public Dictionary<ulong, WS_Handlers_Chat> ChatHandler { get; init; }
    public Dictionary<ulong, WS_Handlers_Gamemaster> GamemasterHandler { get; init; }
    public Dictionary<ulong, WS_Handlers_Instance> InstanceHandler { get; init; }
    public Dictionary<ulong, WS_Handlers_Misc> MiscHandler { get; init; }
    public Dictionary<ulong, WS_Handlers_Taxi> TaxiHandler { get; init; }
    public Dictionary<ulong, WS_Handlers_Trade> TradeHandler { get; init; }
    public Dictionary<ulong, WS_Handlers_Warden> WardenHandler { get; init; }

    public Dictionary<int, List<int>> CreatureQuestStarters { get; init; }
    public Dictionary<int, List<int>> CreatureQuestFinishers { get; init; }
    public Dictionary<int, List<int>> GameobjectQuestStarters { get; init; }
    public Dictionary<int, List<int>> GameobjectQuestFinishers { get; init; }
    public WS_Quests QuestsService { get; init; }
    public WS_GraveYards GraveyardsService { get; init; }

    public Dictionary<ulong, CharacterObject> Characters { get; init; }
    public ReaderWriterLockSlim CharactersLock { get; init; }

    public Dictionary<ulong, WS_Creatures.CreatureObject> WorldCreatures { get; init; }
    public ReaderWriterLockSlim WorldCreaturesLock { get; init; }

    public Dictionary<ulong, WS_DynamicObjects.DynamicObject> WorldDynamicObjects { get; init; }
    public ReaderWriterLockSlim WorldDynamicObjectsLock { get; init; }

    public Dictionary<ulong, WS_Transports.TransportObject> WorldTransports { get; init; }
    public ReaderWriterLockSlim WorldTransportsLock { get; init; }

    public Dictionary<int, WS_Items.ItemInfo> ItemDatabase { get; init; }
    public Dictionary<ulong, ItemObject> WorldItems { get; init; }
    public Dictionary<ulong, GameObject> WorldGameObjects { get; init; }

    public Dictionary<int, CreatureInfo> CreaturesDatabase { get; init; }
    public Dictionary<int, GameObjectInfo> GameObjectsDatabase { get; init; }

    public Dictionary<ulong, WS_Corpses.CorpseObject> WorldCorpseObjects { get; init; }

    public ArrayList WorldCreatureKeys { get; set; }

    public static ulong ItemGuidCounter { get; set; }
    public static ulong CreatureGuidCounter { get; set; }
    public static ulong GameObjectsGuidCounter { get; set; }
    public static ulong CorpseGuidCounter { get; set; }
    public static ulong DynamicObjectsGuidCounter { get; set; }
    public static ulong TransportGuidCounter { get; set; }

    public static Random Rnd { get; } = new Random();
}
