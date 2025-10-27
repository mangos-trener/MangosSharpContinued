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
using Mangos.Logging;
using Mangos.MySql.DependencyInjection;
using Mangos.Tcp;
using Microsoft.Extensions.DependencyInjection;
using RealmServer.DependencyInjection;

Console.Title = "Realm server";

var services = new ServiceCollection()
    .AddConfigurationFile()
    .AddCustomLogging()
    .AddDatabase()
    .AddTcpServer()
    .AddRealmServices();

var serviceProvider = services.BuildServiceProvider();

var configuration = serviceProvider.GetRequiredService<MangosConfiguration>();
var logger = serviceProvider.GetRequiredService<IMangosLogger>();
var tcpServer = serviceProvider.GetRequiredService<TcpServer>();

logger.Trace(@" __  __      _  _  ___  ___  ___               ");
logger.Trace(@"|  \/  |__ _| \| |/ __|/ _ \/ __|   We Love    ");
logger.Trace(@"| |\/| / _` | .` | (_ | (_) \__ \   Vanilla Wow");
logger.Trace(@"|_|  |_\__,_|_|\_|\___|\___/|___/              ");
logger.Trace("                                                ");
logger.Trace("Website / Forum / Support: https://www.getmangos.eu/");

logger.Information("Starting realm tcp server");

await tcpServer.RunAsync(configuration.Realm.RealmServerEndpoint);
