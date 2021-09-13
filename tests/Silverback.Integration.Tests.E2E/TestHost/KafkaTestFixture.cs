// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.TestHost
{
    [Trait("Category", "E2E:Kafka")]
    public abstract class KafkaTestFixture : E2ETestFixture<IKafkaTestingHelper>
    {
        protected const string DefaultTopicName = "default-e2e-topic";

        protected const string DefaultConsumerGroupId = "e2e-consumer-group-1";

        private IInMemoryTopic? _defaultTopic;

        private IMockedConsumerGroup? _defaultConsumerGroup;

        protected KafkaTestFixture(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        protected IInMemoryTopic DefaultTopic => _defaultTopic ??= Helper.GetTopic(DefaultTopicName);

        protected IMockedConsumerGroup DefaultConsumerGroup =>
            _defaultConsumerGroup ??= Helper.GetConsumerGroup(DefaultConsumerGroupId);
    }
}
