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

using Autofac;
using Autofac.Extensions.DependencyInjection;
using Mangos.Configuration;
using Mangos.Logging;
using Mangos.MySql;
using Mangos.Tcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RealmServer.DependencyInjection;
using RealmServer.Services;

Console.Title = "Realm server";

var host = Host.CreateDefaultBuilder(args)

    //
    // TEMPORARY Autofac bridge
    //
    .UseServiceProviderFactory(
        new AutofacServiceProviderFactory())

    //
    // Existing Autofac modules
    //
    .ConfigureContainer<ContainerBuilder>(containerBuilder =>
    {
        containerBuilder.RegisterModule<ConfigurationModule>();
        containerBuilder.RegisterModule<LoggingModule>();
        containerBuilder.RegisterModule<MySqlModule>();
        containerBuilder.RegisterModule<TcpModule>();
        containerBuilder.RegisterModule<RealmModule>();
    })

    //
    // New native registrations
    //
    .ConfigureServices((context, services) => services.AddHostedService<RealmServerHostedService>())
    .Build();

await host.RunAsync();
