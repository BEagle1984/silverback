// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Silverback.Diagnostics;

/// <summary>
///     Contains the <see cref="LogEvent" /> constants of all events logged by the Silverback.Storage.Memory
///     package.
/// </summary>
[SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:Parameter should not span multiple lines", Justification = "Cleaner and clearer this way")]
public static class StorageMemoryLogEvents
{
    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an ambient transaction is detected but it's not
    ///     supported by the in-memory outbox implementation.
    /// </summary>
    public static LogEvent OutboxTransactionUnsupported { get; } = new(
        LogLevel.Warning,
        GetEventId(1, nameof(OutboxTransactionUnsupported)),
        "The ambient transaction is not supported by the in-memory outbox implementation. The messages will be stored outside the transaction.");

    private static EventId GetEventId(int id, string name) =>
        new(6000 + id, $"Silverback.Storage.Memory_{name}");
}
