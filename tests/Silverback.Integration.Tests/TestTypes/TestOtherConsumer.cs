// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Tests.Types;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestOtherConsumer : Consumer<TestOtherBroker, TestOtherConsumerEndpoint, TestOffset>
    {
        public TestOtherConsumer(
            TestOtherBroker broker,
            TestOtherConsumerEndpoint endpoint,
            IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider)
            : base(
                broker,
                endpoint,
                behaviorsProvider,
                serviceProvider,
                serviceProvider.GetRequiredService<ISilverbackIntegrationLogger<TestOtherConsumer>>())
        {
        }

        public int AcknowledgeCount { get; set; }

        protected override Task ConnectCoreAsync() => Task.CompletedTask;

        protected override Task DisconnectCoreAsync() => Task.CompletedTask;

        protected override Task StartCoreAsync() => Task.CompletedTask;

        protected override Task StopCoreAsync() => Task.CompletedTask;

        protected override Task WaitUntilConsumingStoppedAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;

        protected override Task CommitCoreAsync(IReadOnlyCollection<TestOffset> brokerMessageIdentifiers)
        {
            AcknowledgeCount += brokerMessageIdentifiers.Count;
            return Task.CompletedTask;
        }

        protected override Task RollbackCoreAsync(IReadOnlyCollection<TestOffset> brokerMessageIdentifiers) =>
            Task.CompletedTask;
    }
}
