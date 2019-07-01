// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Connectors
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
            var services = new ServiceCollection();

            _outboundQueue = new InMemoryOutboundQueue();

            services
                .AddBus()
                .AddBroker<TestBroker>(options => options
                    .AddDeferredOutboundConnector(_ => _outboundQueue)
                    .AddOutboundConnector());

            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            var serviceProvider = services.BuildServiceProvider();

            _connectorRouter = (OutboundConnectorRouter)serviceProvider.GetServices<ISubscriber>().First(s => s is OutboundConnectorRouter);
            _routingConfiguration = (OutboundRoutingConfiguration)serviceProvider.GetRequiredService<IOutboundRoutingConfiguration>();
            _broker = (TestBroker)serviceProvider.GetRequiredService<IBroker>();

            InMemoryOutboundQueue.Clear();
        }

        [Theory, MemberData(nameof(OnMessageReceived_MultipleMessages_CorrectlyRoutedToEndpoints_TestData))]
        public async Task OnMessageReceived_MultipleMessages_CorrectlyRoutedToEndpoint(IIntegrationMessage message, string[] expectedEndpointNames)
        {
            _routingConfiguration.Add<IIntegrationMessage>(new TestEndpoint("allMessages"), null);
            _routingConfiguration.Add<IIntegrationEvent>(new TestEndpoint("allEvents"), null);
            _routingConfiguration.Add<TestEventOne>(new TestEndpoint("eventOne"), null);
            _routingConfiguration.Add<TestEventTwo>(new TestEndpoint("eventTwo"), null);

            await _connectorRouter.OnMessageReceived(message);
            await _outboundQueue.Commit();

            var queued = await _outboundQueue.Dequeue(100);

            foreach (var expectedEndpointName in expectedEndpointNames)
            {
                queued.Count(x => x.Endpoint.Name == expectedEndpointName).Should().Be(1);
            }

            var notExpectedEndpointNames = _routingConfiguration
                .Routes.Select(r => r.DestinationEndpoint.Name)
                .Where(r => !expectedEndpointNames.Contains(r));

            foreach (var notExpectedEndpointName in notExpectedEndpointNames)
            {
                queued.Count(x => x.Endpoint.Name == notExpectedEndpointName).Should().Be(0);
            }
        }

        public static IEnumerable<object[]> OnMessageReceived_MultipleMessages_CorrectlyRoutedToEndpoints_TestData =>
            new[]
            {
                new object[] { new TestEventOne(), new[] { "allMessages", "allEvents", "eventOne" } },
                new object[] { new TestEventTwo(), new[] { "allMessages", "allEvents", "eventTwo" } }
            };

        [Fact]
        public async Task OnMessageReceived_Message_CorrectlyRoutedToDefaultConnector()
        {
            _routingConfiguration.Add<TestEventOne>(new TestEndpoint("eventOne"), null);

            await _connectorRouter.OnMessageReceived(new TestEventOne());
            await _outboundQueue.Commit();

            var queued = await _outboundQueue.Dequeue(1);
            queued.Count().Should().Be(1);
            _broker.ProducedMessages.Count.Should().Be(0);
        }

        [Fact]
        public async Task OnMessageReceived_Message_CorrectlyRoutedToConnector()
        {
            _routingConfiguration.Add<TestEventOne>(new TestEndpoint("eventOne"), typeof(OutboundConnector));

            await _connectorRouter.OnMessageReceived(new TestEventOne());
            await _outboundQueue.Commit();

            var queued = await _outboundQueue.Dequeue(1);
            queued.Count().Should().Be(0);
            _broker.ProducedMessages.Count.Should().Be(1);
        }
    }
}