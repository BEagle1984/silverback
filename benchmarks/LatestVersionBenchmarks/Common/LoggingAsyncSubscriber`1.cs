// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Silverback.Benchmarks.Latest.Common;

public class LoggingAsyncSubscriber<TMessage>
{
    private readonly ILogger _logger;

    public LoggingAsyncSubscriber(ILogger<LoggingAsyncSubscriber<TMessage>> logger)
    {
        _logger = logger;
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Invoked by Silverback")]
    public async Task HandleMessageAsync(TMessage message)
    {
        _logger.LogDebug("Received message: {Message}", message);

        await Task.Yield();
    }
}
