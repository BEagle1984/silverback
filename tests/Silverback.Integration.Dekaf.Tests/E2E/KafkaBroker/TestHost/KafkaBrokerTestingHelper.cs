// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Testing;

namespace Silverback.Tests.Integration.E2E.KafkaBroker.TestHost;

/// <summary>
///     The <see cref="IKafkaTestingHelper" /> for the tests running against a Kafka container, so that the tests
///     written for the in-memory cluster can run unchanged. The consumer groups are inspected on the broker.
/// </summary>
internal sealed class KafkaBrokerTestingHelper : KafkaTestingHelper, IAsyncDisposable
{
    private readonly IServiceProvider _serviceProvider;

    private readonly KafkaBrokerInspector _inspector;

    private readonly ILogger _logger;

    public KafkaBrokerTestingHelper(IServiceProvider serviceProvider, KafkaContainerHost containerHost, ILogger<KafkaBrokerTestingHelper> logger)
        : base(serviceProvider, null, logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _inspector = new KafkaBrokerInspector(containerHost);
    }

    public ValueTask DisposeAsync() => _inspector.DisposeAsync();

    private protected override IEnumerable<string> GetConsumerGroupIds() =>
        _serviceProvider.GetRequiredService<IConsumerCollection>()
            .OfType<KafkaConsumer>()
            .Select(consumer => consumer.Configuration.GroupId)
            .OfType<string>();

    private protected override IMockedConsumerGroup GetConsumerGroupCore(string groupId) =>
        new KafkaBrokerConsumerGroup(groupId, _inspector, _serviceProvider.GetRequiredService<IConsumerCollection>(), _logger);
}
