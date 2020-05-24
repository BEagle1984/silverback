// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestConsumer : Consumer<TestBroker, TestConsumerEndpoint, TestOffset>
    {
        public TestConsumer(
            TestBroker broker,
            TestConsumerEndpoint endpoint,
            MessagesReceivedAsyncCallback callback,
            IReadOnlyCollection<IConsumerBehavior>? behaviors,
            IServiceProvider serviceProvider,
            ILogger<TestConsumer> logger)
            : base(broker, endpoint, callback, behaviors, serviceProvider, logger)
        {
        }

        public bool IsConnected { get; set; }

        public int AcknowledgeCount { get; set; }

        public Task TestHandleMessage(
            object message,
            IEnumerable<MessageHeader>? headers = null,
            IOffset? offset = null,
            IMessageSerializer? serializer = null) =>
            TestHandleMessage(message, new MessageHeaderCollection(headers), offset, serializer);

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public Task TestConsume(
            byte[]? rawMessage,
            IEnumerable<MessageHeader>? headers = null,
            IOffset? offset = null) =>
            TestHandleMessage(rawMessage, new MessageHeaderCollection(headers), offset);

        public async Task TestHandleMessage(
            object message,
            MessageHeaderCollection headers,
            IOffset? offset = null,
            IMessageSerializer? serializer = null)
        {
            if (serializer == null)
                serializer = new JsonMessageSerializer();

            var buffer = await serializer.SerializeAsync(message, headers, MessageSerializationContext.Empty);

            await TestHandleMessage(buffer, headers, offset);
        }

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public async Task TestHandleMessage(byte[]? rawMessage, MessageHeaderCollection headers, IOffset? offset = null)
        {
            if (!Broker.IsConnected)
                throw new InvalidOperationException("The broker is not connected.");

            if (!IsConnected)
                throw new InvalidOperationException("The consumer is not ready.");

            await HandleMessage(rawMessage, headers, "test-topic", offset);
        }

        public override void Connect() => IsConnected = true;

        public override void Disconnect() => IsConnected = true;

        protected override Task Commit(IReadOnlyCollection<TestOffset> offsets)
        {
            AcknowledgeCount += offsets.Count;
            return Task.CompletedTask;
        }

        protected override Task Rollback(IReadOnlyCollection<TestOffset> offsets)
        {
            // Nothing to do
            return Task.CompletedTask;
        }
    }
}
