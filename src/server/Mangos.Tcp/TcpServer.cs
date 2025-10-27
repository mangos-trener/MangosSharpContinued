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

using Mangos.Logging;
using System.Net;
using System.Net.Sockets;

namespace Mangos.Tcp;

public sealed class TcpServer
{
    private readonly IMangosLogger _logger;
    private readonly ITcpConnection _connection;

    public TcpServer(IMangosLogger logger, ITcpConnection connection)
    {
        _logger = logger;
        _connection = connection;
    }

    public async Task RunAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(IPEndPoint.Parse(endpoint));
        socket.Listen(10);

        _logger.Information($"Tcp server was started on {endpoint}");

        while (!cancellationToken.IsCancellationRequested)
        {
            HandleClientConnection(await socket.AcceptAsync(cancellationToken), cancellationToken);
        }
    }

    private async void HandleClientConnection(Socket socket, CancellationToken cancellationToken)
    {
        if (socket.RemoteEndPoint is not IPEndPoint endpoint)
        {
            _logger.Error("Unable to get remote endpoint");
            return;
        }

        _logger.Information($"Tcp client was conntected {endpoint}");
        try
        {
            await _connection.ExecuteAsync(socket, cancellationToken);
        }
        catch (SocketException exception) when (exception.SocketErrorCode == SocketError.ConnectionAborted)
        {
            _logger.Information("Connection aborted");
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Unhandled exception");
        }
        finally
        {
            socket.Dispose();
        }

        _logger.Information($"Tcp client was disconected");
    }
}
