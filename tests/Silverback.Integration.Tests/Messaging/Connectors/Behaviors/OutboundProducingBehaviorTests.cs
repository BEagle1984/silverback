// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Behaviors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Connectors.Behaviors
{
    [Collection("StaticInMemory")]
    public class OutboundProducingBehaviorTests
    {
        private readonly OutboundProducingBehavior _behavior;
        private readonly InMemoryOutboundQueue _outboundQueue;
        private readonly TestBroker _broker;

        public OutboundProducingBehaviorTests()
        {
            var services = new ServiceCollection();

            _outboundQueue = new InMemoryOutboundQueue();

            services.AddSilverback()
                .WithConnectionTo<TestBroker>(options => options
                    .AddOutboundConnector()
                    .AddDeferredOutboundConnector(_ => _outboundQueue));

            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            var serviceProvider = services.BuildServiceProvider();

            _behavior = (OutboundProducingBehavior) serviceProvider.GetServices<IBehavior>()
                .First(s => s is OutboundProducingBehavior);
            _broker = (TestBroker) serviceProvider.GetRequiredService<IBroker>();

            InMemoryOutboundQueue.Clear();
        }

        [Fact]
        public async Task Handle_OutboundMessage_CorrectlyRelayed()
        {
            var outboundEnvelope = new OutboundEnvelope<TestEventOne>(
                new TestEventOne(),
                new MessageHeader[0],
                new OutboundRoutingConfiguration.OutboundRoute(
                    typeof(TestEventOne),
                    TestProducerEndpoint.GetDefault(),
                    typeof(OutboundConnector)));

            await _behavior.Handle(new[] { outboundEnvelope, outboundEnvelope, outboundEnvelope }, Task.FromResult);
            await _outboundQueue.Commit();

            var queued = await _outboundQueue.Dequeue(10);
            queued.Count().Should().Be(0);
            _broker.ProducedMessages.Count.Should().Be(3);
        }

        [Fact]
        public async Task Handle_OutboundMessage_RelayedViaTheRightConnector()
        {
            var outboundEnvelope = new OutboundEnvelope<TestEventOne>(
                new TestEventOne(),
                new MessageHeader[0],
                new OutboundRoutingConfiguration.OutboundRoute(
                    typeof(TestEventOne),
                    TestProducerEndpoint.GetDefault(),
                    typeof(DeferredOutboundConnector)));

            await _behavior.Handle(new[] { outboundEnvelope, outboundEnvelope, outboundEnvelope }, Task.FromResult);
            await _outboundQueue.Commit();

            var queued = await _outboundQueue.Dequeue(10);
            queued.Count().Should().Be(3);
            _broker.ProducedMessages.Count.Should().Be(0);
        }
    }
}