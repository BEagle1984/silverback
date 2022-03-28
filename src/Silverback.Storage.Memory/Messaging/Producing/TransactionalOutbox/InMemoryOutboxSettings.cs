// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Lock;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     The <see cref="InMemoryOutboxWriter" /> and <see cref="InMemoryOutboxReader" /> settings.
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

    /// <summary>
    ///     Gets the name of the outbox.
    /// </summary>
    public string OutboxName { get; init; } = "default";

    /// <summary>
    ///     Returns an <see cref="InMemoryLockSettings" /> instance using the same database.
    /// </summary>
    /// <returns>
    ///     The <see cref="InMemoryLockSettings" /> instance.
    /// </returns>
    public override DistributedLockSettings GetCompatibleLockSettings() => new InMemoryLockSettings($"outbox.{OutboxName}");

    /// <inheritdoc cref="OutboxSettings.Validate" />
    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrWhiteSpace(OutboxName))
            throw new SilverbackConfigurationException("The outbox name is required.");
    }
}
