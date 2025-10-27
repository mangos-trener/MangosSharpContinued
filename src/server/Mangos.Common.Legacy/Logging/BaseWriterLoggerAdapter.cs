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

using Mangos.Common.Enums.Global;
using Microsoft.Extensions.Logging;
using System;

namespace Mangos.Common.Legacy.Logging;
public class BaseWriterLoggerAdapter : ILogger
{
    private readonly BaseWriter _writer;
    private readonly string _category;

    public BaseWriterLoggerAdapter(BaseWriter writer, string category = "")
    {
        _writer = writer;
        _category = category;
    }

    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => true; // or map to LogType

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                            Exception exception, Func<TState, Exception, string> formatter)
    {
        var message = formatter(state, exception);
        _writer.WriteLine(MapLogLevel(logLevel), $"[{_category}] {message}");
    }

    private LogType MapLogLevel(LogLevel level) =>
        level switch
        {
            LogLevel.Trace => LogType.DEBUG,
            LogLevel.Debug => LogType.DEBUG,
            LogLevel.Information => LogType.INFORMATION,
            LogLevel.Warning => LogType.WARNING,
            LogLevel.Error => LogType.FAILED,
            LogLevel.Critical => LogType.CRITICAL,
            _ => LogType.INFORMATION
        };
}
