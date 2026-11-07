// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.Logging;

namespace Silverback.Diagnostics;

// The StorageMemoryLogEvents properties are generated from the [LoggerMessage] methods, including their documentation
internal static partial class StorageMemoryLoggerExtensions
{
    public static void LogOutboxUnsupportedTransaction(this ISilverbackLogger logger) =>
        OutboxTransactionUnsupported(logger.InnerLogger);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an ambient transaction is detected, but it's not
    ///     supported by the in-memory outbox implementation.
    /// </summary>
    [LoggerMessage(
        6001,
        LogLevel.Warning,
        "Ambient transaction not supported by the in-memory outbox implementation; the messages will be stored ignoring the transaction",
        EventName = "Silverback.Storage.Memory_OutboxTransactionUnsupported")]
    private static partial void OutboxTransactionUnsupported(ILogger logger);
}
