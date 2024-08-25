// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;

namespace Silverback.Diagnostics;

internal static class StorageMemoryExtensions
{
    private static readonly Action<ILogger, Exception?> LogOuboxUnsupportedTransaction =
        SilverbackLoggerMessage.Define(StorageMemoryLogEvents.OutboxTransactionUnsupported);

    public static void LogOutboxUnsupportedTransaction(this ISilverbackLogger logger) =>
        LogOuboxUnsupportedTransaction(logger.InnerLogger, null);
}
