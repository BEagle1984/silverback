// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Silverback.Util;

namespace Silverback.Diagnostics;

internal class SilverbackLogger<TCategoryName> : ISilverbackLogger<TCategoryName>
{
    private readonly IMappedLevelsLogger<TCategoryName> _logger;

    public SilverbackLogger(IMappedLevelsLogger<TCategoryName> mappedLevelsLogger)
    {
        _logger = Check.NotNull(mappedLevelsLogger);
    }

    [SuppressMessage("Minor Code Smell", "S6669:Logger field or property name should comply with a naming convention", Justification = "InnerLogger implements the public ISilverbackLogger member and its name cannot change")]
    public ILogger InnerLogger => _logger;

    public bool IsEnabled(LogEvent logEvent) => _logger.IsEnabled(logEvent.Level, logEvent.EventId);
}
