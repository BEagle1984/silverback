// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class OutboundRoutingBehaviorTests
    {
        private readonly OutboundRoutingBehavior _behavior;
        private readonly OutboundRoutingConfiguration _routingConfiguration;
        private readonly InMemoryOutboundQueue _outboundQueue;
        private readonly TestBroker _broker;

        public OutboundRoutingBehaviorTests()
        {
            var services = new ServiceCollection();

            _outboundQueue = new InMemoryOutboundQueue();

            services.AddSilverback()
                .WithConnectionTo<TestBroker>(options => options
                    .AddDeferredOutboundConnector(_ => _outboundQueue)
                    .AddOutboundConnector());

            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            var serviceProvider = services.BuildServiceProvider();

            _behavior = (OutboundRoutingBehavior) serviceProvider.GetServices<IBehavior>()
                .First(s => s is OutboundRoutingBehavior);
            _routingConfiguration =
                (OutboundRoutingConfiguration) serviceProvider.GetRequiredService<IOutboundRoutingConfiguration>();
            _broker = (TestBroker) serviceProvider.GetRequiredService<IBroker>();

            InMemoryOutboundQueue.Clear();
        }

        [Theory, MemberData(nameof(Handle_MultipleMessages_CorrectlyRoutedToEndpoints_TestData))]
        public async Task Handle_MultipleMessages_CorrectlyRoutedToEndpoint(IIntegrationMessage message,
            string[] expectedEndpointNames)
        {
            _routingConfiguration.Add<IIntegrationMessage>(new TestProducerEndpoint("allMessages"), null);
            _routingConfiguration.Add<IIntegrationEvent>(new TestProducerEndpoint("allEvents"), null);
            _routingConfiguration.Add<TestEventOne>(new TestProducerEndpoint("eventOne"), null);
            _routingConfiguration.Add<TestEventTwo>(new TestProducerEndpoint("eventTwo"), null);

            await _behavior.Handle(new[] {message}, Task.FromResult);
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

        public static IEnumerable<object[]> Handle_MultipleMessages_CorrectlyRoutedToEndpoints_TestData =>
            new[]
            {
                new object[] { new TestEventOne(), new[] { "allMessages", "allEvents", "eventOne" } },
                new object[] { new TestEventTwo(), new[] { "allMessages", "allEvents", "eventTwo" } }
            };

        [Fact]
        public async Task Handle_Message_CorrectlyRoutedToDefaultConnector()
        {
            _routingConfiguration.Add<TestEventOne>(new TestProducerEndpoint("eventOne"), null);

            await _behavior.Handle(new[] { new TestEventOne() }, Task.FromResult);
            await _outboundQueue.Commit();

            var queued = await _outboundQueue.Dequeue(1);
            queued.Count().Should().Be(1);
            _broker.ProducedMessages.Count.Should().Be(0);
        }

        [Fact]
        public async Task Handle_Message_CorrectlyRoutedToConnector()
        {
            _routingConfiguration.Add<TestEventOne>(new TestProducerEndpoint("eventOne"), typeof(OutboundConnector));

            await _behavior.Handle(new[] { new TestEventOne() }, Task.FromResult);
            await _outboundQueue.Commit();

            var queued = await _outboundQueue.Dequeue(1);
            queued.Count().Should().Be(0);
            _broker.ProducedMessages.Count.Should().Be(1);
        }

        [Fact]
        public async Task Handle_Messages_RoutedMessageIsFiltered()
        {
            _routingConfiguration.Add<TestEventOne>(new TestProducerEndpoint("eventOne"), typeof(OutboundConnector));

            var messages = await _behavior.Handle(new object[] { new TestEventOne(), new TestEventTwo() }, Task.FromResult);

            messages.Count().Should().Be(1);
            messages.First().Should().NotBeOfType<TestEventOne>();
        }
        
        [Fact]
        public async Task Handle_MessagesWithPublishToInternBusOption_RoutedMessageIsNotFiltered()
        {
            _routingConfiguration.PublishOutboundMessagesToInternalBus = true;
            _routingConfiguration.Add<TestEventOne>(new TestProducerEndpoint("eventOne"), typeof(OutboundConnector));

            var messages = await _behavior.Handle(new object[] { new TestEventOne(), new TestEventTwo() }, Task.FromResult);

            messages.Count().Should().Be(2);
        }

        [Fact]
        // Test for possible issue similar to #33: messages don't have to be registered with HandleMessagesOfType to be relayed
        public async Task Handle_UnhandledMessageType_CorrectlyRelayed()
        {
            var message = new SomeUnhandledMessage { Content = "abc" };
            _routingConfiguration.Add<SomeUnhandledMessage>(new TestProducerEndpoint("eventOne"), typeof(OutboundConnector));

            await _behavior.Handle(new[] { message }, Task.FromResult);

            _broker.ProducedMessages.Count.Should().Be(1);
        }
        
        
        [Fact]
        public async Task Handle_InboundMessages_AreIgnored()
        {
            _routingConfiguration.Add<TestEventOne>(new TestProducerEndpoint("eventOne"), null);

            var messages = new[] { new TestEventOne(), new TestEventOne() };
            var wrappedMessages = messages.Select(message =>
                new InboundMessage<TestEventOne>(new byte[1], null, null, TestConsumerEndpoint.GetDefault(), true)
                {
                    Content = message
                }).ToList();
            
            await _behavior.Handle(wrappedMessages, Task.FromResult);
            await _behavior.Handle(messages, Task.FromResult);
            await _outboundQueue.Commit();

            var queued = await _outboundQueue.Dequeue(1);
            queued.Count().Should().Be(0);
            _broker.ProducedMessages.Count.Should().Be(0);
        }
    }
}