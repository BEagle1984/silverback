// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="InMemoryKafkaOffsetStoreSettings" />.
/// </summary>
public class InMemoryKafkaOffsetStoreSettingsBuilder : IKafkaOffsetStoreSettingsImplementationBuilder
{
    private string? _offsetStoreName;

    /// <summary>
    ///     Sets the offset store name.
    /// </summary>
    /// <param name="offsetStoreName">
    ///     The name of the offset store.
    /// </param>
    /// <returns>
    ///     The <see cref="InMemoryKafkaOffsetStoreSettingsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public InMemoryKafkaOffsetStoreSettingsBuilder WithName(string offsetStoreName)
    {
        _offsetStoreName = Check.NotNullOrEmpty(offsetStoreName, nameof(offsetStoreName));
        return this;
    }

    /// <inheritdoc cref="IKafkaOffsetStoreSettingsImplementationBuilder.Build" />
    public KafkaOffsetStoreSettings Build()
    {
        InMemoryKafkaOffsetStoreSettings settings = _offsetStoreName != null ? new InMemoryKafkaOffsetStoreSettings(_offsetStoreName) : new InMemoryKafkaOffsetStoreSettings();

        settings.Validate();

        return settings;
    }
}
