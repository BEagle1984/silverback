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
using Silverback.Messaging.Outbound.Routing;
using Silverback.Tests.Types;
using Silverback.Util;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestProducer : Producer<TestBroker, TestProducerConfiguration, TestProducerEndpoint>
    {
        public TestProducer(
            TestBroker broker,
            TestProducerConfiguration settings,
            IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
            IOutboundEnvelopeFactory envelopeFactory,
            IServiceProvider serviceProvider)
            : base(
                broker,
                settings,
                behaviorsProvider,
                envelopeFactory,
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
            TestProducerEndpoint endpoint) =>
            ProduceCore(
                message,
                messageStream.ReadAll(),
                headers,
                endpoint);

        protected override IBrokerMessageIdentifier? ProduceCore(
            object? message,
            byte[]? messageBytes,
            IReadOnlyCollection<MessageHeader>? headers,
            TestProducerEndpoint endpoint)
        {
            ProducedMessages.Add(new ProducedMessage(messageBytes, headers, endpoint));
            return null;
        }

        protected override void ProduceCore(
            object? message,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers,
            TestProducerEndpoint endpoint,
            Action<IBrokerMessageIdentifier?> onSuccess,
            Action<Exception> onError)
        {
            ProduceCore(
                message,
                messageStream.ReadAll(),
                headers,
                endpoint,
                onSuccess,
                onError);
        }

        protected override void ProduceCore(
            object? message,
            byte[]? messageBytes,
            IReadOnlyCollection<MessageHeader>? headers,
            TestProducerEndpoint endpoint,
            Action<IBrokerMessageIdentifier?> onSuccess,
            Action<Exception> onError)
        {
            ProducedMessages.Add(new ProducedMessage(messageBytes, headers, endpoint));
            onSuccess.Invoke(null);
        }

        protected override async Task<IBrokerMessageIdentifier?> ProduceCoreAsync(
            object? message,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers,
            TestProducerEndpoint endpoint) =>
            await ProduceCoreAsync(
                message,
                await messageStream.ReadAllAsync().ConfigureAwait(false),
                headers,
                endpoint);

        protected override Task<IBrokerMessageIdentifier?> ProduceCoreAsync(
            object? message,
            byte[]? messageBytes,
            IReadOnlyCollection<MessageHeader>? headers,
            TestProducerEndpoint endpoint)
        {
            ProducedMessages.Add(new ProducedMessage(messageBytes, headers, endpoint));
            return Task.FromResult<IBrokerMessageIdentifier?>(null);
        }

        protected override async Task ProduceCoreAsync(
            object? message,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers,
            TestProducerEndpoint endpoint,
            Action<IBrokerMessageIdentifier?> onSuccess,
            Action<Exception> onError) =>
            await ProduceCoreAsync(
                message,
                await messageStream.ReadAllAsync().ConfigureAwait(false),
                headers,
                endpoint,
                onSuccess,
                onError);

        protected override Task ProduceCoreAsync(
            object? message,
            byte[]? messageBytes,
            IReadOnlyCollection<MessageHeader>? headers,
            TestProducerEndpoint endpoint,
            Action<IBrokerMessageIdentifier?> onSuccess,
            Action<Exception> onError)
        {
            ProducedMessages.Add(new ProducedMessage(messageBytes, headers, endpoint));
            onSuccess.Invoke(null);
            return Task.CompletedTask;
        }
    }
}
