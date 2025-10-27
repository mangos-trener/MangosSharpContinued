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
using Mangos.Logging.DependencyInjection;
using Mangos.MySql.DependencyInjection;
using Mangos.Tcp;
using Microsoft.Extensions.DependencyInjection;
using RealmServer.Domain;
using RealmServer.Handlers;
using RealmServer.Network;
using RealmServer.Requests;
using System.Text.Json;

namespace RealmServer.DependencyInjection;

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

        return services;
    }

    public static IServiceCollection AddRealmServices(this IServiceCollection services)
    {
        services.AddScoped<ITcpConnection, RealmTcpConnection>();
        services.AddScoped<ClientState>();

        services.AddScoped<RsLogonChallengeHandler>();
        services.AddScoped<RsLogonProofHandler>();
        services.AddScoped<AuthReconnectChallengeHandler>();
        services.AddScoped<AuthRealmlistHandler>();

        services.AddScoped<IHandlerDispatcher, HandlerDispatcher<RsLogonChallengeHandler, RsLogonChallengeRequest>>();
        services.AddScoped<IHandlerDispatcher, HandlerDispatcher<RsLogonProofHandler, RsLogonProofRequest>>();
        services.AddScoped<IHandlerDispatcher, HandlerDispatcher<AuthReconnectChallengeHandler, RsLogonChallengeRequest>>();
        services.AddScoped<IHandlerDispatcher, HandlerDispatcher<AuthRealmlistHandler, AuthRealmlistRequest>>();

        return services;
    }
}
