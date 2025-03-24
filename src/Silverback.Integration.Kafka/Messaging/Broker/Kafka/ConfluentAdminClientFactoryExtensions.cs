// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Confluent.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     Adds some convenience methods to the <see cref="IConfluentAdminClientFactory" /> interface.
/// </summary>
public static class ConfluentAdminClientFactoryExtensions
{
    /// <summary>
    ///     Creates a new <see cref="IAdminClient" /> instance using the specified configuration.
    /// </summary>
    /// <param name="factory">
    ///     The <see cref="IConfluentAdminClientFactory" />.
    /// </param>
    /// <param name="builderAction">
    ///     An <see cref="Action{T}" /> that takes a <see cref="KafkaClientConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="IAdminClient" />.
    /// </returns>
    public static IAdminClient GetClient(this IConfluentAdminClientFactory factory, Action<KafkaClientConfigurationBuilder> builderAction)
    {
        Check.NotNull(builderAction, nameof(builderAction));

        KafkaClientConfigurationBuilder builder = new();
        builderAction.Invoke(builder);

        return factory.GetClient(builder.Build());
    }

    /// <summary>
    ///     Creates a new <see cref="IAdminClient" /> instance using the specified configuration.
    /// </summary>
    /// <param name="factory">
    ///     The <see cref="IConfluentAdminClientFactory" />.
    /// </param>
    /// <param name="configuration">
    ///     The configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="IAdminClient" />.
    /// </returns>
    public static IAdminClient GetClient(this IConfluentAdminClientFactory factory, KafkaClientConfiguration configuration) =>
        Check.NotNull(factory, nameof(factory)).GetClient(Check.NotNull(configuration, nameof(configuration)).ToConfluentConfig());

    /// <summary>
    ///     Creates a new <see cref="IAdminClient" /> instance using the specified configuration.
    /// </summary>
    /// <param name="factory">
    ///     The <see cref="IConfluentAdminClientFactory" />.
    /// </param>
    /// <param name="configuration">
    ///     The configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="IAdminClient" />.
    /// </returns>
    public static IAdminClient GetClient(this IConfluentAdminClientFactory factory, KafkaProducerConfiguration configuration)
    {
        Check.NotNull(configuration, nameof(configuration));

        ClientConfig cleanedUpConfig = CleanUpConfig(configuration.ToConfluentConfig(), "dotnet.producer.");

        return Check.NotNull(factory, nameof(factory)).GetClient(cleanedUpConfig);
    }

    /// <summary>
    ///     Creates a new <see cref="IAdminClient" /> instance using the specified configuration.
    /// </summary>
    /// <param name="factory">
    ///     The <see cref="IConfluentAdminClientFactory" />.
    /// </param>
    /// <param name="configuration">
    ///     The configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="IAdminClient" />.
    /// </returns>
    public static IAdminClient GetClient(this IConfluentAdminClientFactory factory, KafkaConsumerConfiguration configuration)
    {
        Check.NotNull(configuration, nameof(configuration));

        ClientConfig cleanedUpConfig = CleanUpConfig(configuration.ToConfluentConfig(), "dotnet.consumer.");

        return Check.NotNull(factory, nameof(factory)).GetClient(cleanedUpConfig);
    }

    private static ClientConfig CleanUpConfig(ClientConfig config, string prefixToDiscard)
    {
        Dictionary<string, string> cleanedUpConfig = [];

        foreach (KeyValuePair<string, string> keyValuePair in config)
        {
            if (keyValuePair.Key.StartsWith(prefixToDiscard, StringComparison.Ordinal))
                continue;

            cleanedUpConfig.Add(keyValuePair.Key, keyValuePair.Value);
        }

        return new ClientConfig(cleanedUpConfig);
    }
}
