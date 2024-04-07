// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <seealso cref="UseMemory" /> method to the <see cref="OutboxSettingsBuilder" />.
/// </summary>
public static class OutboxSettingsBuilderMemoryExtensions
{
    /// <summary>
    ///     Configures the in-memory outbox.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="OutboxSettingsBuilder" />.
    /// </param>
    /// <returns>
    ///     The <see cref="InMemoryOutboxSettingsBuilder" />.
    /// </returns>
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Extension method (fluent API)")]
    public static InMemoryOutboxSettingsBuilder UseMemory(this OutboxSettingsBuilder builder) => new();
}
