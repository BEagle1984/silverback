// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.Logging;
using Silverback.Util;

namespace Silverback.Diagnostics;

internal class SilverbackLogger<TCategoryName> : ISilverbackLogger<TCategoryName>
{
    private readonly IMappedLevelsLogger<TCategoryName> _mappedLevelsLogger;

    public SilverbackLogger(IMappedLevelsLogger<TCategoryName> mappedLevelsLogger)
    {
        _mappedLevelsLogger = Check.NotNull(mappedLevelsLogger, nameof(mappedLevelsLogger));
    }

    public ILogger InnerLogger => _mappedLevelsLogger;

    public bool IsEnabled(LogEvent logEvent) =>
        _mappedLevelsLogger.IsEnabled(logEvent.Level, logEvent.EventId, logEvent.Message);
}
