// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <seealso cref="UseMemory"/> method to the <see cref="OutboxSettingsBuilder" />.
/// </summary>
public static class OutboxSettingsBuilderMemoryExtensions
{
    /// <summary>
    ///     Configures the outbox to be stored in memory.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="OutboxSettingsBuilder"/>.
    /// </param>
    /// <returns>
    ///     The <see cref="IOutboxSettingsImplementationBuilder"/>.
    /// </returns>
    public static InMemoryOutboxSettingsBuilder UseMemory(this OutboxSettingsBuilder builder) => new();
}
