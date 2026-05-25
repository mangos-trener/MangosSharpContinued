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
using Mangos.Tcp;
using Microsoft.Extensions.Hosting;

namespace RealmServer.Services;

public sealed class RealmServerHostedService : IHostedService
{
    private readonly MangosConfiguration _configuration;
    private readonly IMangosLogger _logger;
    private readonly TcpServer _tcpServer;

    public RealmServerHostedService(
        MangosConfiguration configuration,
        IMangosLogger logger,
        TcpServer tcpServer)
    {
        _configuration = configuration;
        _logger = logger;
        _tcpServer = tcpServer;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.Trace(@" __  __      _  _  ___  ___  ___               ");
        _logger.Trace(@"|  \/  |__ _| \| |/ __|/ _ \/ __|   We Love    ");
        _logger.Trace(@"| |\/| / _` | .` | (_ | (_) \__ \   Vanilla Wow");
        _logger.Trace(@"|_|  |_\__,_|_|\_|\___|\___/|___/              ");
        _logger.Trace("                                                ");
        _logger.Trace("Website / Forum / Support: https://www.getmangos.eu/");

        _logger.Information("Starting realm tcp server");

        await _tcpServer.RunAsync(
            _configuration.Realm.RealmServerEndpoint);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.Information("Realm server stopping");

        return Task.CompletedTask;
    }
}
