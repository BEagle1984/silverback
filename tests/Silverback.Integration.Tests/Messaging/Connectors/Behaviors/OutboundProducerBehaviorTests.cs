// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound;
using Silverback.Messaging.Outbound.Deferred;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Connectors.Behaviors
{
    public class OutboundProducerBehaviorTests
    {
        private readonly ProduceBehavior _behavior;

        private readonly InMemoryOutboundQueue _outboundQueue;

        private readonly TestBroker _broker;

        public OutboundProducerBehaviorTests()
        {
            var services = new ServiceCollection();

            services.AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>()
                        .AddOutboundConnector()
                        .AddDeferredOutboundConnector<InMemoryOutboundQueue>());

            services.AddNullLogger();

            var serviceProvider = services.BuildServiceProvider();

            _behavior = (ProduceBehavior)serviceProvider.GetServices<IBehavior>()
                .First(behavior => behavior is ProduceBehavior);
            _broker = serviceProvider.GetRequiredService<TestBroker>();
            _outboundQueue = (InMemoryOutboundQueue)serviceProvider.GetRequiredService<IOutboundQueueWriter>();
        }

        [Fact]
        public async Task Handle_OutboundMessage_CorrectlyRelayed()
        {
            var outboundEnvelope = new OutboundEnvelope<TestEventOne>(
                new TestEventOne(),
                Array.Empty<MessageHeader>(),
                TestProducerEndpoint.GetDefault(),
                typeof(OutboundConnector));

            await _behavior.Handle(new[] { outboundEnvelope, outboundEnvelope, outboundEnvelope }, Task.FromResult!);
            await _outboundQueue.Commit();

            var queued = await _outboundQueue.Dequeue(10);
            queued.Count.Should().Be(0);
            _broker.ProducedMessages.Count.Should().Be(3);
        }

        [Fact]
        public async Task Handle_OutboundMessage_RelayedViaTheRightConnector()
        {
            var outboundEnvelope = new OutboundEnvelope<TestEventOne>(
                new TestEventOne(),
                Array.Empty<MessageHeader>(),
                TestProducerEndpoint.GetDefault(),
                typeof(DeferredOutboundConnector));

            await _behavior.Handle(new[] { outboundEnvelope, outboundEnvelope, outboundEnvelope }, Task.FromResult!);
            await _outboundQueue.Commit();

            var queued = await _outboundQueue.Dequeue(10);
            queued.Count.Should().Be(3);
            _broker.ProducedMessages.Count.Should().Be(0);
        }
    }
}
