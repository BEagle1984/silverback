// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Testing;
using Silverback.Tests.Integration.E2E.TestTypes;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.TestHost
{
    [Trait("Category", "E2E")]
    public abstract class E2ETestFixture : IDisposable
    {
        protected const string DefaultTopicName = "default-e2e-topic";

        private SpyBrokerBehavior? _spyBrokerBehavior;

        private OutboundInboundSubscriber? _outboundInboundSubscriber;

        private IBroker? _broker;

        private IInMemoryTopic? _defaultTopic;

        private IKafkaTestingHelper? _kafkaTestingHelper;

        protected E2ETestFixture(ITestOutputHelper testOutputHelper)
        {
            Host = new TestApplicationHost().WithTestOutputHelper(testOutputHelper);
        }

        protected TestApplicationHost Host { get; }

        protected SpyBrokerBehavior SpyBehavior => _spyBrokerBehavior ??=
            Host.ScopedServiceProvider.GetServices<IBrokerBehavior>().OfType<SpyBrokerBehavior>().First();

        protected OutboundInboundSubscriber Subscriber => _outboundInboundSubscriber ??=
            Host.ScopedServiceProvider.GetRequiredService<OutboundInboundSubscriber>();

        protected IBroker Broker => _broker ??= Host.ScopedServiceProvider.GetRequiredService<IBroker>();

        protected IInMemoryTopic DefaultTopic => _defaultTopic ??= GetTopic(DefaultTopicName);

        protected IKafkaTestingHelper KafkaTestingHelper =>
            _kafkaTestingHelper ??= Host.ScopedServiceProvider.GetRequiredService<IKafkaTestingHelper>();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected IInMemoryTopic GetTopic(string name) =>
            Host.ScopedServiceProvider.GetRequiredService<IInMemoryTopicCollection>()[name];

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            Host.Dispose();
        }
    }
}
