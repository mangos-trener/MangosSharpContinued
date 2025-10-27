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

using GameServer.Handlers;
using GameServer.Network;
using GameServer.Requests;
using GameServer.Services;
using Mangos.Cluster;
using Mangos.Cluster.DataStores;
using Mangos.Cluster.Globals;
using Mangos.Cluster.Handlers;
using Mangos.Cluster.Handlers.Guild;
using Mangos.Cluster.Interfaces;
using Mangos.Cluster.Network;
using Mangos.Common.Enums.Global;
using Mangos.Common.Legacy;
using Mangos.Common.Legacy.Logging;
using Mangos.Configuration;
using Mangos.DataStores;
using Mangos.Logging.DependencyInjection;
using Mangos.MySql.DependencyInjection;
using Mangos.Tcp;
using Mangos.World;
using Mangos.World.Auction;
using Mangos.World.Battlegrounds;
using Mangos.World.DataStores;
using Mangos.World.Gossip;
using Mangos.World.Handlers;
using Mangos.World.Loots;
using Mangos.World.Maps;
using Mangos.World.Network;
using Mangos.World.Objects;
using Mangos.World.Objects.Factories;
using Mangos.World.Objects.Factories.Client;
using Mangos.World.Objects.Factories.Combat;
using Mangos.World.Objects.Factories.CreaturesAi;
using Mangos.World.Objects.Factories.Gossip;
using Mangos.World.Objects.Factories.Groups;
using Mangos.World.Objects.Factories.Loot;
using Mangos.World.Objects.Factories.Maps;
using Mangos.World.Objects.Factories.Packets;
using Mangos.World.Objects.Factories.Quests;
using Mangos.World.Objects.Factories.Spells;
using Mangos.World.Objects.Factories.Warden;
using Mangos.World.Objects.Factories.Weather;
using Mangos.World.Player;
using Mangos.World.Quests;
using Mangos.World.Scripts;
using Mangos.World.Server;
using Mangos.World.Services;
using Mangos.World.Social;
using Mangos.World.Spells;
using Mangos.World.Warden;
using Mangos.World.Weather;
using Mangos.Zip;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using static Mangos.World.Warden.WS_Warden;

namespace GameServer.DependencyInjection;
public static class DependencyInjection
{
    private const string ConfigurationFileName = "configuration.json";

    public static IServiceCollection AddConfigurationFile(this IServiceCollection services)
    {
        services.AddSingleton(provider =>
        {
            if (!File.Exists(ConfigurationFileName))
                throw new FileNotFoundException($"Unable to locate {ConfigurationFileName}");

            var json = File.ReadAllText(ConfigurationFileName);
            var config = JsonSerializer.Deserialize<MangosConfiguration>(json)
                ?? throw new Exception($"Unable to deserialize {ConfigurationFileName}");

            return config;
        });

        return services;
    }

    public static IServiceCollection AddCustomLogging(this IServiceCollection services)
    {
        services.AddSingleton<ILoggerProvider>(sp =>
        {
            var config = sp.GetRequiredService<MangosConfiguration>();

            var writer = BaseWriter.CreateLog(config.World.LogType, config.World.LogConfig);
            writer.LogLevel = LogType.INFORMATION;

            return new BaseWriterLoggerProvider(writer);
        });

        services.AddMangosLogger();

        return services;
    }

    public static IServiceCollection AddMySqlDatabase(this IServiceCollection services)
    {
        services.AddDatabase();

        return services;
    }

    public static IServiceCollection AddTcpServer(this IServiceCollection services)
    {
        services.AddSingleton<TcpServer>();
        services.AddSingleton<ICluster, WorldServerClass>();

        return services;
    }

    public static IServiceCollection AddGameModule(this IServiceCollection services)
    {
        services.AddScoped<ITcpConnection, GameTcpConnection>();
        services.AddScoped<IGameState, GameState>();

        services.AddSingleton<Mangos.World.Globals.ScriptedObject>();
        services.AddSingleton<IScriptExecutor>(sp =>
        {
            var scriptedObject = sp.GetRequiredService<Mangos.World.Globals.ScriptedObject>();
            return new ScriptExecutor(scriptedObject);
        });

        services.AddScoped<CMSG_PING_Handler>();
        services.AddScoped<IHandlerDispatcher, HandlerDispatcher<CMSG_PING, CMSG_PING_Handler>>();

        return services;
    }

    public static IServiceCollection AddLegacyClusterServices(this IServiceCollection services)
    {
        services.AddSingleton<ClientClass>();

        services.AddSingleton<DataStoreProvider>();
        services.AddSingleton<ZipService>();
        services.AddSingleton<Mangos.World.Warden.NativeMethods>();
        services.AddSingleton<LegacyWorldCluster>();
        services.AddSingleton<IClusterContext>(sp => sp.GetRequiredService<LegacyWorldCluster>());
        services.AddSingleton<WsDbcDatabase>();
        services.AddSingleton<WsDbcLoad>();
        services.AddSingleton<Packets>();
        services.AddSingleton<WcGuild>();
        services.AddSingleton<WcNetwork>();
        services.AddSingleton<WcHandlers>();
        services.AddSingleton<WcHandlersAuth>();
        services.AddSingleton<WcHandlersBattleground>();
        services.AddSingleton<WcHandlersChat>();
        services.AddSingleton<WcHandlersGroup>();
        services.AddSingleton<WcHandlersGuild>();
        services.AddSingleton<WcHandlersMisc>();
        services.AddSingleton<WcHandlersMovement>();
        services.AddSingleton<WcHandlersSocial>();
        services.AddSingleton<WcHandlersTickets>();
        services.AddSingleton<WsHandlerChannels>();
        services.AddSingleton<WcHandlerCharacter>();
        services.AddSingleton<WcHandlersGuild>();
        services.AddSingleton<WcHandlersGuild>();
        services.AddSingleton<WcHandlersGuild>();

        return services;
    }

    public static IServiceCollection AddLegacyWorldServices(this IServiceCollection services)
    {
        services.AddSingleton<ZipService>();
        services.AddSingleton<WorldServer>();
        services.AddSingleton<WorldState>();
        services.AddSingleton<DataStoreProvider>();

        services.AddSingleton<Mangos.World.AI.WS_Creatures_AI>();
        services.AddSingleton<WS_Auction>();
        services.AddSingleton<WS_Battlegrounds>();
        services.AddSingleton<WS_DBCDatabase>();
        services.AddSingleton<WS_DBCLoad>();
        services.AddSingleton<Packets>();
        services.AddSingleton<WS_GuardGossip>();
        services.AddSingleton<WS_Loot>();
        services.AddSingleton<WS_Maps>();
        services.AddSingleton<WS_Corpses>();
        services.AddSingleton<WS_Creatures>();
        services.AddSingleton<WS_DynamicObjects>();
        services.AddSingleton<WS_GameObjects>();
        services.AddSingleton<WS_Items>();
        services.AddSingleton<WS_NPCs>();
        services.AddSingleton<IQuestsService, WS_Quests>();
        services.AddSingleton<WS_Quests>();
        services.AddSingleton<WS_Pets>();
        services.AddSingleton<WS_Transports>();
        services.AddSingleton<CharManagementHandler>();
        services.AddSingleton<WS_CharMovement>();
        services.AddSingleton<WS_Combat>();
        services.AddSingleton<WS_Commands>();
        services.AddSingleton<WS_Spawns>();
        services.AddSingleton<WS_Handlers>();
        services.AddSingleton<WS_Handlers_Battleground>();
        services.AddSingleton<WS_Handlers_Chat>();
        services.AddSingleton<WS_Handlers_Gamemaster>();
        services.AddSingleton<WS_Handlers_Instance>();
        services.AddSingleton<WS_Handlers_Misc>();
        services.AddSingleton<WS_Handlers_Taxi>();
        services.AddSingleton<WS_Handlers_Trade>();
        services.AddSingleton<WS_Handlers_Warden>();
        services.AddSingleton<WS_Player_Creation>();
        services.AddSingleton<WS_Player_Initializator>();
        services.AddSingleton<WS_PlayerHelper>();
        services.AddSingleton<WS_Network>();
        services.AddSingleton<WS_TimerBasedEvents>();
        services.AddSingleton<WS_Group>();
        services.AddSingleton<WS_Guilds>();
        services.AddSingleton<WS_Mail>();
        services.AddSingleton<WS_Spells>();
        services.AddSingleton<WS_Warden>();
        services.AddSingleton<WS_Weather>();
        services.AddSingleton<WS_GraveYards>();
        services.AddSingleton<WS_Network.WorldServerClass>();
        services.AddSingleton<WS_TimerBasedEvents.TAIManager>();
        services.AddSingleton<WS_TimerBasedEvents.TWeatherChanger>();
        services.AddSingleton<WS_TimerBasedEvents.TCharacterSaver>();
        services.AddSingleton<WS_TimerBasedEvents.TRegenerator>();
        services.AddSingleton<WS_TimerBasedEvents.TSpellManager>();
        services.AddSingleton<WardenMaiev>();

        services.AddSingleton<ICharacterResurrectionService, CharacterResurrectionService>();
        services.AddSingleton<ICellUpdater, CellUpdater>();
        services.AddSingleton<IMapTileLoader, MapTileLoader>();

        services.AddSingleton<Func<WS_Spawns>>(sp => () => sp.GetRequiredService<WS_Spawns>());

        return services;
    }

    public static IServiceCollection AddFactories(this IServiceCollection services)
    {
        services.AddSingleton<ClientClassFactory>();
        services.AddSingleton<UpdateClassFactory>();

        services.AddSingleton<DynamicObjectFactory>();

        services.AddSingleton<GameObjectFactory>();
        services.AddSingleton<GameObjectInfoFactory>();

        services.AddSingleton<CharacterObjectFactory>();
        services.AddSingleton<CreatureObjectFactory>();
        services.AddSingleton<CreatureInfoFactory>();
        services.AddSingleton<PetObjectFactory>();
        services.AddSingleton<CorpseObjectFactory>();
        services.AddSingleton<TotemObjectFactory>();

        services.AddSingleton<ItemObjectFactory>();
        services.AddSingleton<ItemInfoFactory>();

        services.AddSingleton<BaseActiveSpellFactory>();
        services.AddSingleton<SpellTargetsFactory>();
        services.AddSingleton<SpellInfoFactory>();
        services.AddSingleton<CastSpellParametersFactory>();
        services.AddSingleton<SpellEffectFactory>();

        services.AddSingleton<MapFactory>();
        services.AddSingleton<MapTileFactory>();

        services.AddSingleton<WeatherZoneFactory>();

        services.AddSingleton<DefaultTalkFactory>();
        services.AddSingleton<NpcTextFactory>();
        services.AddSingleton<GuardTalkFactory>();

        services.AddSingleton<TransportObjectFactory>();

        services.AddSingleton<TradeInfoFactory>();

        services.AddSingleton<GroupFactory>();

        services.AddSingleton<LootObjectFactory>();
        services.AddSingleton<LootItemFactory>();
        services.AddSingleton<LootTemplateFactory>();
        services.AddSingleton<LootGroupFactory>();
        services.AddSingleton<LootStoreFactory>();
        services.AddSingleton<GroupLootInfoFactory>();

        services.AddSingleton<BaseQuestFactory>();
        services.AddSingleton<QuestsFactory>();
        services.AddSingleton<IQuestInfoFactory, QuestInfoFactory>();
        services.AddSingleton<CritterAiFactory>();
        services.AddSingleton<DefaultAiFactory>();
        services.AddSingleton<GuardAiFactory>();
        services.AddSingleton<GuardWaypointAiFactory>();
        services.AddSingleton<PetAiFactory>();
        services.AddSingleton<StandStillAiFactory>();
        services.AddSingleton<WaypointAiFactory>();

        services.AddSingleton<AttackTimerFactory>();

        services.AddSingleton<WardenScanFactory>();

        services.AddSingleton<Func<LootObjectFactory>>(sp => () => sp.GetRequiredService<LootObjectFactory>());
        services.AddSingleton<Func<ItemObjectFactory>>(sp => () => sp.GetRequiredService<ItemObjectFactory>());

        return services;
    }
}
