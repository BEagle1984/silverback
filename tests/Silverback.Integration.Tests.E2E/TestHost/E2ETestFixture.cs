// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Topics;
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

        // TODO: Remove
        protected E2ETestFixture()
        {
            Host = new TestApplicationHost();
        }

        protected E2ETestFixture(ITestOutputHelper testOutputHelper)
        {
            Host = new TestApplicationHost().WithTestOutputHelper(testOutputHelper);
        }

        protected TestApplicationHost Host { get; }

        protected SpyBrokerBehavior SpyBehavior => _spyBrokerBehavior ??=
            Host.ServiceProvider.GetServices<IBrokerBehavior>().OfType<SpyBrokerBehavior>().First();

        protected OutboundInboundSubscriber Subscriber => _outboundInboundSubscriber ??=
            Host.ServiceProvider.GetRequiredService<OutboundInboundSubscriber>();

        protected IBroker Broker => Host.ServiceProvider.GetRequiredService<IBroker>();

        protected IInMemoryTopic DefaultTopic => GetTopic(DefaultTopicName);

        protected IKafkaTestingHelper KafkaTestingHelper => Host.ServiceProvider.GetRequiredService<IKafkaTestingHelper>();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected IInMemoryTopic GetTopic(string name) =>
            Host.ServiceProvider.GetRequiredService<IInMemoryTopicCollection>()[name];

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            Host.Dispose();
        }
    }
}
