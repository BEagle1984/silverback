// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     The builder for the <see cref="MockedConfluentAdminClient" />.
/// </summary>
public class MockedConfluentAdminClientFactory : ConfluentAdminClientFactoryBase
{
    private readonly IInMemoryTopicCollection _topics;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MockedConfluentAdminClientFactory" /> class.
    /// </summary>
    /// <param name="topics">
    ///     The <see cref="IInMemoryTopicCollection" />.
    /// </param>
    /// <param name="producers">
    ///     The <see cref="IProducerCollection" />.
    /// </param>
    /// <param name="consumers">
    ///     The <see cref="IConsumerCollection" />.
    /// </param>
    public MockedConfluentAdminClientFactory(IInMemoryTopicCollection topics, IProducerCollection producers, IConsumerCollection consumers)
        : base(producers, consumers)
    {
        _topics = Check.NotNull(topics, nameof(topics));
    }

    /// <inheritdoc cref="ConfluentAdminClientFactoryBase.GetClient(AdminClientConfig)" />
    public override IAdminClient GetClient(AdminClientConfig config) =>
        new MockedConfluentAdminClient(Check.NotNull(config, nameof(config)), _topics);
}
