// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;

namespace Silverback.Diagnostics;

internal sealed class LogLevelOverride
{
    public LogLevelOverride(LogLevel fixedLevel)
    {
        FixedLevel = fixedLevel;
        GetLogLevel = (_, _, _) => fixedLevel;
    }

    public LogLevelOverride(Func<Exception?, LogLevel, Lazy<string>, LogLevel> logLevelFunc)
    {
        GetLogLevel = logLevelFunc;
    }

    public LogLevel? FixedLevel { get; }

    public Func<Exception?, LogLevel, Lazy<string>, LogLevel> GetLogLevel { get; }
}
