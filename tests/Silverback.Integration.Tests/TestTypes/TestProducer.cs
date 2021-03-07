// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Silverback.Util;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestProducer : Producer<TestBroker, TestProducerEndpoint>
    {
        public TestProducer(
            TestBroker broker,
            TestProducerEndpoint endpoint,
            IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider)
            : base(
                broker,
                endpoint,
                behaviorsProvider,
                serviceProvider,
                Substitute.For<IOutboundLogger<TestProducer>>())
        {
            ProducedMessages = broker.ProducedMessages;
        }

        public IList<ProducedMessage> ProducedMessages { get; }

        protected override IBrokerMessageIdentifier? ProduceCore(
            object? message,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName) =>
            ProduceCore(
                message,
                messageStream.ReadAll(),
                headers,
                actualEndpointName);

        protected override IBrokerMessageIdentifier? ProduceCore(
            object? message,
            byte[]? messageBytes,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName)
        {
            ProducedMessages.Add(new ProducedMessage(messageBytes, headers, Endpoint));
            return null;
        }

        protected override void ProduceCore(
            object? message,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName,
            Action<IBrokerMessageIdentifier?> onSuccess,
            Action<Exception> onError)
        {
            ProduceCore(
                message,
                messageStream.ReadAll(),
                headers,
                actualEndpointName,
                onSuccess,
                onError);
        }

        protected override void ProduceCore(
            object? message,
            byte[]? messageBytes,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName,
            Action<IBrokerMessageIdentifier?> onSuccess,
            Action<Exception> onError)
        {
            ProducedMessages.Add(new ProducedMessage(messageBytes, headers, Endpoint));
            onSuccess.Invoke(null);
        }

        protected override async Task<IBrokerMessageIdentifier?> ProduceCoreAsync(
            object? message,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName) =>
            await ProduceCoreAsync(
                message,
                await messageStream.ReadAllAsync().ConfigureAwait(false),
                headers,
                actualEndpointName);

        protected override Task<IBrokerMessageIdentifier?> ProduceCoreAsync(
            object? message,
            byte[]? messageBytes,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName)
        {
            ProducedMessages.Add(new ProducedMessage(messageBytes, headers, Endpoint));
            return Task.FromResult<IBrokerMessageIdentifier?>(null);
        }

        protected override async Task ProduceCoreAsync(
            object? message,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName,
            Action<IBrokerMessageIdentifier?> onSuccess,
            Action<Exception> onError) =>
            await ProduceCoreAsync(
                message,
                await messageStream.ReadAllAsync().ConfigureAwait(false),
                headers,
                actualEndpointName,
                onSuccess,
                onError);

        protected override Task ProduceCoreAsync(
            object? message,
            byte[]? messageBytes,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName,
            Action<IBrokerMessageIdentifier?> onSuccess,
            Action<Exception> onError)
        {
            ProducedMessages.Add(new ProducedMessage(messageBytes, headers, Endpoint));
            onSuccess.Invoke(null);
            return Task.CompletedTask;
        }
    }
}
