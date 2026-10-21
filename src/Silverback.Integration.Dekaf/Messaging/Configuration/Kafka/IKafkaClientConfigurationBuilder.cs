// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Exposes the settings shared by producers and consumers, so that <see cref="KafkaClientsConfigurationBuilder" />
///     can apply them to every client.
/// </summary>
/// <remarks>
///     When adding a shared setting:
///     1. Add the property to <see cref="KafkaClientConfiguration" />
///     2. Add the method here and implement it in <see cref="KafkaClientConfigurationBuilder{TConfig,TBuilder}" />
///     3. Forward it in <see cref="KafkaClientsConfigurationBuilder" />
///     4. Map it in <c>DekafClientsFactory</c>.
/// </remarks>
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internal interface")]
internal interface IKafkaClientConfigurationBuilder
{
    void WithBootstrapServers(string? bootstrapServers);

    void WithClientId(string? clientId);
}
