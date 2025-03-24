// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <seealso cref="UseMemory" /> method to the <see cref="KafkaOffsetStoreSettingsBuilder" />.
/// </summary>
public static class KafkaOffsetStoreSettingsBuilderMemoryExtensions
{
    /// <summary>
    ///     Configures the in-memory offset store.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="KafkaOffsetStoreSettingsBuilder" />.
    /// </param>
    /// <returns>
    ///     The <see cref="InMemoryKafkaOffsetStoreSettingsBuilder" />.
    /// </returns>
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Extension method (fluent API)")]
    public static InMemoryKafkaOffsetStoreSettingsBuilder UseMemory(this KafkaOffsetStoreSettingsBuilder builder) => new();
}
