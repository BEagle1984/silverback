// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     The builder for the <see cref="MockedConfluentAdminClient" />.
/// </summary>
public class MockedConfluentAdminClientBuilder : IConfluentAdminClientBuilder
{
    private readonly IMockedKafkaOptions _options;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MockedConfluentAdminClientBuilder" /> class.
    /// </summary>
    /// <param name="options">
    ///     The <see cref="IMockedKafkaOptions" />.
    /// </param>
    public MockedConfluentAdminClientBuilder(IMockedKafkaOptions options)
    {
        _options = Check.NotNull(options, nameof(options));
    }

    /// <inheritdoc cref="IConfluentAdminClientBuilder.Build" />
    public IAdminClient Build(ClientConfig config) =>
        new MockedConfluentAdminClient(
            Check.NotNull(config, nameof(config)),
            _options);
}
