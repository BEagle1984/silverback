// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
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

        private IInMemoryTopic? _defaultTopic;

        protected KafkaTestFixture(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        protected IInMemoryTopic DefaultTopic => _defaultTopic ??= GetTopic(DefaultTopicName);

        protected IInMemoryTopic GetTopic(string name) =>
            Host.ScopedServiceProvider.GetRequiredService<IInMemoryTopicCollection>()[name];
    }
}
