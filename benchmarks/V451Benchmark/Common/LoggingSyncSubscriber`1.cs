// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Silverback.Benchmarks.V451.Common;

public class LoggingSyncSubscriber<TMessage>
{
    private readonly ILogger _logger;

    public LoggingSyncSubscriber(ILogger<LoggingSyncSubscriber<TMessage>> logger)
    {
        _logger = logger;
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Invoked by Silverback")]
    public void HandleMessage(TMessage message) => _logger.LogDebug("Received message: {Message}", message);
}
