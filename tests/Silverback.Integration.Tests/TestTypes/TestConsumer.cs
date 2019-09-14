// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestConsumer : Consumer
    {
        public TestConsumer(IBroker broker, IEndpoint endpoint)
            : base(broker, endpoint)
        {
        }

        public bool IsReady { get; set; }

        public int AcknowledgeCount { get; set; }

        public Task TestPush(object message, IEnumerable<MessageHeader> headers = null, IOffset offset = null,
            IMessageSerializer serializer = null) =>
            TestPush(message, new MessageHeaderCollection(headers), offset, serializer);

        public Task TestPush(byte[] rawMessage, IEnumerable<MessageHeader> headers = null, IOffset offset = null) =>
            TestPush(rawMessage, new MessageHeaderCollection(headers), offset);

        public async Task TestPush(object message, MessageHeaderCollection headers, IOffset offset = null,
            IMessageSerializer serializer = null)
        {
            if (serializer == null)
                serializer = new JsonMessageSerializer();

            var buffer = serializer.Serialize(message, headers);

            await TestPush(buffer, headers, offset);
        }

        public async Task TestPush(byte[] rawMessage, MessageHeaderCollection headers, IOffset offset = null)
        {
            if (!Broker.IsConnected)
                throw new InvalidOperationException("The broker is not connected.");

            if (!IsReady)
                throw new InvalidOperationException("The consumer is not ready.");

            await HandleMessage(rawMessage, headers, offset);
        }

#pragma warning disable 1998
        public override async Task Acknowledge(IEnumerable<IOffset> offsets) =>
            AcknowledgeCount += offsets.Count();
#pragma warning restore 1998
    }
}