// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestConsumer : Consumer
    {
        static DiagnosticListener _diagnosticsListener = new DiagnosticListener("test");

        public TestConsumer(IBroker broker, IEndpoint endpoint)
            : base(broker, endpoint, new NullLogger<TestConsumer>(), new MessageLogger(new MessageKeyProvider(Enumerable.Empty<IMessageKeyProvider>())), _diagnosticsListener)
        {
        }

        public bool IsReady { get; set; }

        public int AcknowledgeCount { get; set; }

        public void TestPush(object message, IEnumerable<MessageHeader> headers = null, IOffset offset = null, IMessageSerializer serializer = null)
        {
            if (!Broker.IsConnected)
                throw new InvalidOperationException("The broker is not connected.");

            if (!IsReady)
                throw new InvalidOperationException("The consumer is not ready.");

            if (serializer == null)
                serializer = new JsonMessageSerializer();

            var buffer = serializer.Serialize(message);

            HandleMessage(buffer, headers, offset);
        }

        public override void Acknowledge(IEnumerable<IOffset> offsets) =>
            AcknowledgeCount = AcknowledgeCount + offsets.Count();
    }
}