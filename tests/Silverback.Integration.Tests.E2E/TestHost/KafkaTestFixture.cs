// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.TestHost
{
    [Trait("Category", "Kafka")]
    public abstract class KafkaTestFixture : E2ETestFixture
    {
        protected const string DefaultTopicName = "default-e2e-topic";

        private IInMemoryTopic? _defaultTopic;

        private IKafkaTestingHelper? _testingHelper;

        protected KafkaTestFixture(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        protected IInMemoryTopic DefaultTopic => _defaultTopic ??= GetTopic(DefaultTopicName);

        protected IKafkaTestingHelper TestingHelper =>
            _testingHelper ??= Host.ScopedServiceProvider.GetRequiredService<IKafkaTestingHelper>();

        protected IInMemoryTopic GetTopic(string name) =>
            Host.ScopedServiceProvider.GetRequiredService<IInMemoryTopicCollection>()[name];
    }
}
