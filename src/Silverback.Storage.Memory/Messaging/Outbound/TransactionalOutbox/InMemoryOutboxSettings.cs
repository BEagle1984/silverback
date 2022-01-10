// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Outbound.TransactionalOutbox;

/// <summary>
///     The <see cref="InMemoryOutboxWriter" /> and <see cref="InMemoryOutboxReader"/> settings.
/// </summary>
public record InMemoryOutboxSettings : OutboxSettings
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="InMemoryOutboxSettings" /> class.
    /// </summary>
    public InMemoryOutboxSettings()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="InMemoryOutboxSettings" /> class.
    /// </summary>
    /// <param name="outboxName">
    ///     The name of the outbox.
    /// </param>
    public InMemoryOutboxSettings(string outboxName)
    {
        OutboxName = outboxName;
    }

    public string OutboxName { get; init; } = "default";
}
