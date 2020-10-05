// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Topics;
using Silverback.Tests.Integration.E2E.TestTypes;

namespace Silverback.Tests.Integration.E2E.TestHost
{
    public class E2ETestFixture : IDisposable
    {
        protected const string DefaultTopicName = "default-e2e-topic";

        private SpyBrokerBehavior? _spyBrokerBehavior;

        private OutboundInboundSubscriber? _outboundInboundSubscriber;

        protected TestApplicationHost Host { get; } = new TestApplicationHost();

        protected SpyBrokerBehavior SpyBehavior => _spyBrokerBehavior ??=
            Host.ServiceProvider.GetServices<IBrokerBehavior>().OfType<SpyBrokerBehavior>().First();

        protected OutboundInboundSubscriber Subscriber => _outboundInboundSubscriber ??=
            Host.ServiceProvider.GetRequiredService<OutboundInboundSubscriber>();

        protected IBroker Broker => Host.ServiceProvider.GetRequiredService<IBroker>();

        protected IInMemoryTopic DefaultTopic => GetTopic(DefaultTopicName);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected IInMemoryTopic GetTopic(string name) =>
            Host.ServiceProvider.GetRequiredService<IInMemoryTopicCollection>()[name];

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Host.Dispose();
            }
        }
    }
}
