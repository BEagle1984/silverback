// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <seealso cref="UseMemory" /> method to the <see cref="KafkaOffsetStoreSettingsBuilder" />.
/// </summary>
public static class KafkaOffsetStoreSettingsBuilderMemoryExtensions
{
    /// <summary>
    ///     Configures the offsetStore to be stored in memory.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="KafkaOffsetStoreSettingsBuilder" />.
    /// </param>
    /// <returns>
    ///     The <see cref="InMemoryKafkaOffsetStoreSettingsBuilder" />.
    /// </returns>
    public static InMemoryKafkaOffsetStoreSettingsBuilder UseMemory(this KafkaOffsetStoreSettingsBuilder builder) => new();
}
