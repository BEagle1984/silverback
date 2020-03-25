// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestConsumer : Consumer<TestBroker, TestConsumerEndpoint, TestOffset>
    {
        public TestConsumer(
            TestBroker broker,
            TestConsumerEndpoint endpoint,
            IEnumerable<IConsumerBehavior> behaviors,
            ILogger<TestConsumer> logger)
            : base(broker, endpoint, behaviors, logger)
        {
        }

        public bool IsConnected { get; set; }

        public int AcknowledgeCount { get; set; }

        public Task TestPush(
            object message,
            IEnumerable<MessageHeader> headers = null,
            IOffset offset = null,
            IMessageSerializer serializer = null) =>
            TestPush(message, new MessageHeaderCollection(headers), offset, serializer);

        public Task TestPush(byte[] rawMessage, IEnumerable<MessageHeader> headers = null, IOffset offset = null) =>
            TestPush(rawMessage, new MessageHeaderCollection(headers), offset);

        public async Task TestPush(
            object message,
            MessageHeaderCollection headers,
            IOffset offset = null,
            IMessageSerializer serializer = null)
        {
            if (serializer == null)
                serializer = new JsonMessageSerializer();

            var buffer = serializer.Serialize(message, headers, MessageSerializationContext.Empty);

            await TestPush(buffer, headers, offset);
        }

        public async Task TestPush(byte[] rawMessage, MessageHeaderCollection headers, IOffset offset = null)
        {
            if (!Broker.IsConnected)
                throw new InvalidOperationException("The broker is not connected.");

            if (!IsConnected)
                throw new InvalidOperationException("The consumer is not ready.");

            await HandleMessage(rawMessage, headers, "test-topic", offset);
        }

        protected override Task Commit(IEnumerable<TestOffset> offsets)
        {
            AcknowledgeCount += offsets.Count();
            return Task.CompletedTask;
        }

        protected override Task Rollback(IEnumerable<TestOffset> offsets)
        {
            // Nothing to do
            return Task.CompletedTask;
        }

        public override void Connect() => IsConnected = true;

        public override void Disconnect() => IsConnected = true;
    }
}