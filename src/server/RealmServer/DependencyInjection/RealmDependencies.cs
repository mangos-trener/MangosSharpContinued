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

using Mangos.Tcp;
using Microsoft.Extensions.DependencyInjection;
using RealmServer.Domain;
using RealmServer.Handlers;
using RealmServer.Network;
using RealmServer.Requests;

namespace RealmServer.DependencyInjection;
public static class RealmDependencies
{
    public static IServiceCollection AddRealm(
        this IServiceCollection services)
    {
        // Core services
        services.AddScoped<ITcpConnection, RealmTcpConnection>();
        services.AddScoped<ClientState>();

        // Handlers
        RegisterHandlers(services);

        // Dispatchers
        RegisterDispatchers(services);

        return services;
    }

    private static void RegisterHandlers(
        IServiceCollection services)
    {
        services.AddScoped<RsLogonChallengeHandler>();
        services.AddScoped<RsLogonProofHandler>();
        services.AddScoped<AuthReconnectChallengeHandler>();
        services.AddScoped<AuthRealmlistHandler>();
    }

    private static void RegisterDispatchers(
        IServiceCollection services)
    {
        services.AddScoped<IHandlerDispatcher,
            HandlerDispatcher<RsLogonChallengeHandler,
                RsLogonChallengeRequest>>();

        services.AddScoped<IHandlerDispatcher,
            HandlerDispatcher<RsLogonProofHandler,
                RsLogonProofRequest>>();

        services.AddScoped<IHandlerDispatcher,
            HandlerDispatcher<AuthReconnectChallengeHandler,
                RsLogonChallengeRequest>>();

        services.AddScoped<IHandlerDispatcher,
            HandlerDispatcher<AuthRealmlistHandler,
                AuthRealmlistRequest>>();
    }
}
