// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Configuration;

namespace Silverback.Messaging.Consuming.KafkaOffsetStore;

/// <summary>
///     The <see cref="IKafkaOffsetStore" /> settings.
/// </summary>
public abstract record KafkaOffsetStoreSettings : IValidatableSettings
{
    /// <inheritdoc cref="IValidatableSettings.Validate" />
    public virtual void Validate()
    {
    }
}
