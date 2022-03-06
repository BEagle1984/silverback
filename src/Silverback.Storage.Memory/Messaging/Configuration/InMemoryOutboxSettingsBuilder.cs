// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="InMemoryOutboxSettings" />.
/// </summary>
public class InMemoryOutboxSettingsBuilder : IOutboxSettingsImplementationBuilder
{
    private string? _outboxName;

    /// <summary>
    ///     Sets the outbox name.
    /// </summary>
    /// <param name="outboxName">
    ///     The name of the outbox.
    /// </param>
    /// <returns>
    ///     The <see cref="InMemoryOutboxSettingsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public InMemoryOutboxSettingsBuilder WithName(string outboxName)
    {
        _outboxName = Check.NotNullOrEmpty(outboxName, nameof(outboxName));
        return this;
    }

    /// <inheritdoc cref="IOutboxSettingsImplementationBuilder.Build" />
    public OutboxSettings Build()
    {
        InMemoryOutboxSettings settings = new();

        if (_outboxName != null)
            settings = settings with { OutboxName = _outboxName };

        settings.Validate();

        return settings;
    }
}
