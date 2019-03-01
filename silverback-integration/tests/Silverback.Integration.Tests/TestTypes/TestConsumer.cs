// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Tests.TestTypes
{
    public class TestConsumer : Consumer
    {
        public TestConsumer(IBroker broker, IEndpoint endpoint)
            : base(broker, endpoint, new NullLogger<TestConsumer>(), new MessageLogger(new MessageKeyProvider(Enumerable.Empty<IMessageKeyProvider>())))
        {
        }

        public bool IsReady { get; set; }

        public int AcknowledgeCount { get; set; }

        public void TestPush(object message, IOffset offset = null, IMessageSerializer serializer = null)
        {
            if (!Broker.IsConnected)
                throw new InvalidOperationException("The broker is not connected.");

            if (!IsReady)
                throw new InvalidOperationException("The consumer is not ready.");

            if (serializer == null)
                serializer = new JsonMessageSerializer();

            var buffer = serializer.Serialize(message);

            HandleMessage(buffer, offset);
        }

        public override void Acknowledge(IEnumerable<IOffset> offsets) =>
            AcknowledgeCount = AcknowledgeCount + offsets.Count();
    }
}