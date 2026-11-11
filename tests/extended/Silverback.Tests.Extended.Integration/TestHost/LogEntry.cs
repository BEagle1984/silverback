// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;

namespace Silverback.Tests.Extended.Integration.TestHost;

public sealed record LogEntry(LogLevel Level, EventId EventId, string Message, Exception? Exception)
{
    public override string ToString() =>
        Exception == null
            ? $"{Level} {EventId.Id}: {Message}"
            : $"{Level} {EventId.Id}: {Message} | {Exception.GetType().Name}: {Exception.Message}";
}
