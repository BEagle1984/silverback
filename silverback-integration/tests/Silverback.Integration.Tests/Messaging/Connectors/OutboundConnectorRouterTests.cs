// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Messaging.Connectors
{
    [Collection("StaticInMemory")]
    public class OutboundConnectorRouterTests
    {
        private readonly OutboundConnectorRouter _connectorRouter;
        private readonly OutboundRoutingConfiguration _routingConfiguration;
        private readonly InMemoryOutboundQueue _outboundQueue;
        private readonly TestBroker _broker;

        public OutboundConnectorRouterTests()
        {
            _broker = new TestBroker();

            _outboundQueue = new InMemoryOutboundQueue();

            var outboundConnector = new OutboundConnector(_broker);
            var deferredOutboundConnector = new DeferredOutboundConnector(_outboundQueue,
                new NullLogger<DeferredOutboundConnector>(),
                new MessageLogger(new MessageKeyProvider(new[] {new DefaultPropertiesMessageKeyProvider()})));

            _routingConfiguration = new OutboundRoutingConfiguration();
            _connectorRouter = new OutboundConnectorRouter(
                _routingConfiguration,
                new IOutboundConnector[]
                {
                    deferredOutboundConnector,
                    outboundConnector
                });

            InMemoryOutboundQueue.Clear();
        }

        [Theory, ClassData(typeof(OnMessageReceived_MultipleMessages_CorrectlyRoutedToEndpoints_TestData))]
        public async Task OnMessageReceived_MultipleMessages_CorrectlyRoutedToEndpoint(IIntegrationMessage message, string[] expectedEndpointNames)
        {
            _routingConfiguration.Add<IIntegrationMessage>(new TestEndpoint("allMessages"), null);
            _routingConfiguration.Add<IIntegrationEvent>(new TestEndpoint("allEvents"), null);
            _routingConfiguration.Add<TestEventOne>(new TestEndpoint("eventOne"), null);
            _routingConfiguration.Add<TestEventTwo>(new TestEndpoint("eventTwo"), null);

            await _connectorRouter.OnMessageReceived(message);
            await _outboundQueue.Commit();

            var enqueued = _outboundQueue.Dequeue(100);

            foreach (var expectedEndpointName in expectedEndpointNames)
            {
                enqueued.Count(x => x.Endpoint.Name == expectedEndpointName).Should().Be(1);
            }

            var notExpectedEndpointNames = _routingConfiguration
                .Routes.Select(r => r.DestinationEndpoint.Name)
                .Where(r => !expectedEndpointNames.Contains(r));

            foreach (var notExpectedEndpointName in notExpectedEndpointNames)
            {
                enqueued.Count(x => x.Endpoint.Name == notExpectedEndpointName).Should().Be(0);
            }
        }

        [Fact]
        public async Task OnMessageReceived_Message_CorrectlyRoutedToDefaultConnector()
        {
            _routingConfiguration.Add<TestEventOne>(new TestEndpoint("eventOne"), null);

            await _connectorRouter.OnMessageReceived(new TestEventOne());
            await _outboundQueue.Commit();

            var queued = _outboundQueue.Dequeue(1);
            queued.Count().Should().Be(1);
            _broker.ProducedMessages.Count.Should().Be(0);
        }

        [Fact]
        public async Task OnMessageReceived_Message_CorrectlyRoutedToConnector()
        {
            _routingConfiguration.Add<TestEventOne>(new TestEndpoint("eventOne"), typeof(OutboundConnector));

            await _connectorRouter.OnMessageReceived(new TestEventOne());
            await _outboundQueue.Commit();

            var queued = _outboundQueue.Dequeue(1);
            queued.Count().Should().Be(0);
            _broker.ProducedMessages.Count.Should().Be(1);
        }
    }
}