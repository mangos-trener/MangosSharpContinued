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

using Microsoft.Extensions.Logging;

namespace Mangos.Logging;
public class MangosLoggerAdapter : IMangosLogger
{
    private readonly ILogger _logger;

    public MangosLoggerAdapter(ILogger<MangosLoggerAdapter> logger)
    {
        _logger = logger;
    }

    public void Trace(string message) =>
        _logger.LogTrace(message);

    public void Trace(Exception exception, string message) =>
        _logger.LogTrace(exception, message);

    public void Information(string message) =>
        _logger.LogInformation(message);

    public void Information(Exception exception, string message) =>
        _logger.LogInformation(exception, message);

    public void Warning(string message) =>
        _logger.LogWarning(message);

    public void Warning(Exception exception, string message) =>
        _logger.LogWarning(exception, message);

    public void Error(string message) =>
        _logger.LogError(message);

    public void Error(Exception exception, string message) =>
        _logger.LogError(exception, message);
}
