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

using GameServer.DependencyInjection;
using Mangos.Cluster;
using Mangos.Common.Legacy.Globals;
using Mangos.Configuration;
using Mangos.Logging;
using Mangos.Tcp;
using Mangos.World;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Console.Title = "Game server";

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddConfigurationFile()
            .AddCustomLogging()
            .AddMySqlDatabase()
            .AddTcpServer()
            .AddGameModule()
            .AddLegacyClusterServices()
            .AddLegacyWorldServices()
            .AddFactories();
    })
    .Build();

var legacyGlobalFunctionsLogger = host.Services.GetRequiredService<IMangosLogger>();
LegacyGlobalFunctions.InitializeLogger(legacyGlobalFunctionsLogger);

var configuration = host.Services.GetRequiredService<MangosConfiguration>();
var logger = host.Services.GetRequiredService<IMangosLogger>();
var tcpServer = host.Services.GetRequiredService<TcpServer>();
var legacyWorldCluster = host.Services.GetRequiredService<LegacyWorldCluster>();
//WorldServiceLocator.Container = host.Services;
var worldServer = host.Services.GetRequiredService<WorldServer>();

logger.Trace(@" __  __      _  _  ___  ___  ___               ");
logger.Trace(@"|  \/  |__ _| \| |/ __|/ _ \/ __|   We Love    ");
logger.Trace(@"| |\/| / _` | .` | (_ | (_) \__ \   Vanilla Wow");
logger.Trace(@"|_|  |_\__,_|_|\_|\___|\___/|___/              ");
logger.Trace("                                                ");
logger.Trace("Website / Forum / Support: https://www.getmangos.eu/");

logger.Information("Starting legacy cluster server");
await legacyWorldCluster.StartAsync();

logger.Information("Starting legacy world server");
//await worldServer.StartAsync();

logger.Information("Starting game tcp server");
await tcpServer.RunAsync(configuration.Cluster.ClusterServerEndpoint);
