// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestOtherConsumer : Consumer<TestOtherBroker, TestOtherConsumerEndpoint, TestOffset>
    {
        public TestOtherConsumer(
            TestOtherBroker broker,
            TestOtherConsumerEndpoint endpoint,
            MessagesReceivedAsyncCallback callback,
            IReadOnlyCollection<IConsumerBehavior> behaviors,
            IServiceProvider serviceProvider,
            ILogger<TestOtherConsumer> logger)
            : base(broker, endpoint, callback, behaviors, serviceProvider, logger)
        {
        }

        public bool IsConnected { get; set; }

        public int AcknowledgeCount { get; set; }

        protected override Task Commit(IReadOnlyCollection<TestOffset> offsets)
        {
            AcknowledgeCount += offsets.Count();
            return Task.CompletedTask;
        }

        protected override Task Rollback(IReadOnlyCollection<TestOffset> offsets)
        {
            // Nothing to do
            return Task.CompletedTask;
        }

        public override void Connect() => IsConnected = true;

        public override void Disconnect() => IsConnected = true;
    }
}