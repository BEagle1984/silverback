// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Util;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestConsumer : Consumer<TestBroker, TestConsumerEndpoint, TestOffset>
    {
        public TestConsumer(
            TestBroker broker,
            TestConsumerEndpoint endpoint,
            IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider)
            : base(
                broker,
                endpoint,
                behaviorsProvider,
                serviceProvider,
                serviceProvider.GetRequiredService<IInboundLogger<TestConsumer>>())
        {
        }

        public int AcknowledgeCount { get; set; }

        public Task TestHandleMessage(
            object message,
            IReadOnlyCollection<MessageHeader>? headers = null,
            TestOffset? offset = null,
            IMessageSerializer? serializer = null) =>
            TestHandleMessage(message, new MessageHeaderCollection(headers), offset, serializer);

        public Task TestConsume(
            byte[]? rawMessage,
            IReadOnlyCollection<MessageHeader>? headers = null,
            TestOffset? offset = null) =>
            TestHandleMessage(rawMessage, new MessageHeaderCollection(headers), offset);

        public async Task TestHandleMessage(
            object message,
            MessageHeaderCollection? headers,
            TestOffset? offset = null,
            IMessageSerializer? serializer = null)
        {
            if (serializer == null)
                serializer = new JsonMessageSerializer();

            headers ??= new MessageHeaderCollection();

            var stream = await serializer.SerializeAsync(message, headers, MessageSerializationContext.Empty);
            var buffer = await stream.ReadAllAsync();

            await TestHandleMessage(buffer, headers, offset);
        }

        public async Task TestHandleMessage(
            byte[]? rawMessage,
            MessageHeaderCollection headers,
            TestOffset? offset = null)
        {
            if (!Broker.IsConnected)
                throw new InvalidOperationException("The broker is not connected.");

            if (!IsConnected)
                throw new InvalidOperationException("The consumer is not ready.");

            await HandleMessageAsync(
                rawMessage,
                headers,
                "test-topic",
                offset ?? new TestOffset());
        }

        protected override Task ConnectCoreAsync() => Task.CompletedTask;

        protected override Task DisconnectCoreAsync() => Task.CompletedTask;

        protected override Task StartCoreAsync() => Task.CompletedTask;

        protected override Task StopCoreAsync() => Task.CompletedTask;

        protected override Task WaitUntilConsumingStoppedAsync() =>
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
