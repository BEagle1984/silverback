// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Behaviors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Connectors.Behaviors
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class OutboundRouterBehaviorTests
    {
        private readonly OutboundRouterBehavior _behavior;
        private readonly IOutboundRoutingConfiguration _routingConfiguration;
        private readonly InMemoryOutboundQueue _outboundQueue;
        private readonly TestBroker _broker;
        private readonly TestOtherBroker _otherBroker;
        private readonly TestSubscriber _testSubscriber;

        public OutboundRouterBehaviorTests()
        {
            var services = new ServiceCollection();

            _outboundQueue = new InMemoryOutboundQueue(new TransactionalListSharedItems<QueuedMessage>());
            _testSubscriber = new TestSubscriber();

            services.AddSingleton<IOutboundQueueWriter>(_outboundQueue);

            services.AddSilverback()
                .WithConnectionToMessageBroker(options => options
                    .AddBroker<TestBroker>()
                    .AddBroker<TestOtherBroker>()
                    .AddDeferredOutboundConnector()
                    .AddOutboundConnector());

            services.AddSingletonSubscriber(_testSubscriber);

            services.AddNullLogger();

            var serviceProvider = services.BuildServiceProvider();

            _behavior = (OutboundRouterBehavior) serviceProvider.GetServices<IBehavior>()
                .First(s => s is OutboundRouterBehavior);
            _routingConfiguration =
                (OutboundRoutingConfiguration) serviceProvider.GetRequiredService<IOutboundRoutingConfiguration>();
            _broker = serviceProvider.GetRequiredService<TestBroker>();
            _otherBroker = serviceProvider.GetRequiredService<TestOtherBroker>();
        }

        [Theory, MemberData(nameof(Handle_MultipleMessages_CorrectlyRoutedToEndpoints_TestData))]
        public async Task Handle_MultipleMessages_CorrectlyRoutedToStaticEndpoint(
            IIntegrationMessage message,
            string[] expectedEndpointNames)
        {
            _routingConfiguration.Add<IIntegrationMessage>(
                new StaticOutboundRouter(new TestProducerEndpoint("allMessages")));
            _routingConfiguration.Add<IIntegrationEvent>(
                new StaticOutboundRouter(new TestProducerEndpoint("allEvents")));
            _routingConfiguration.Add<TestEventOne>(new StaticOutboundRouter(new TestProducerEndpoint("eventOne")));
            _routingConfiguration.Add<TestEventTwo>(new StaticOutboundRouter(new TestProducerEndpoint("eventTwo")));

            await _behavior.Handle(new[] { message }, Task.FromResult);
            await _outboundQueue.Commit();

            var queued = await _outboundQueue.Dequeue(100);

            foreach (var expectedEndpointName in expectedEndpointNames)
            {
                queued.Count(x => x.Endpoint.Name == expectedEndpointName).Should().Be(1);
            }

            var notExpectedEndpointNames = _routingConfiguration
                .Routes.Select(r => r.Router.Endpoints.First().Name)
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
            _routingConfiguration.Add<TestEventOne>(new StaticOutboundRouter(new TestProducerEndpoint("eventOne")));

            await _behavior.Handle(new[] { new TestEventOne() }, Task.FromResult);
            await _outboundQueue.Commit();

            var queued = await _outboundQueue.Dequeue(1);
            queued.Count.Should().Be(1);
            _broker.ProducedMessages.Count.Should().Be(0);
        }

        [Fact]
        public async Task Handle_Message_CorrectlyRoutedToConnector()
        {
            _routingConfiguration.Add<TestEventOne>(new StaticOutboundRouter(new TestProducerEndpoint("eventOne")),
                typeof(OutboundConnector));

            await _behavior.Handle(new[] { new TestEventOne() }, Task.FromResult);
            await _outboundQueue.Commit();

            var queued = await _outboundQueue.Dequeue(1);
            queued.Count.Should().Be(0);
            _broker.ProducedMessages.Count.Should().Be(1);
        }

        [Fact]
        public async Task Handle_Messages_RoutedMessageIsFiltered()
        {
            _routingConfiguration.Add<TestEventOne>(new StaticOutboundRouter(new TestProducerEndpoint("eventOne")),
                typeof(OutboundConnector));

            var messages =
                await _behavior.Handle(new object[] { new TestEventOne(), new TestEventTwo() }, Task.FromResult);

            messages.Count.Should().Be(1);
            messages.First().Should().NotBeOfType<TestEventOne>();
        }

        [Fact]
        public async Task Handle_Messages_RoutedMessageIsRepublishedWithoutAutoUnwrap()
        {
            _routingConfiguration.Add<TestEventOne>(new StaticOutboundRouter(new TestProducerEndpoint("eventOne")),
                typeof(OutboundConnector));

            await _behavior.Handle(new object[] { new TestEventOne(), new TestEventTwo() }, Task.FromResult);

            _testSubscriber.ReceivedMessages.Count.Should().Be(0); // Because TestSubscriber discards the envelopes
        }

        [Fact]
        public async Task Handle_MessagesWithPublishToInternBusOption_RoutedMessageIsFiltered()
        {
            _routingConfiguration.PublishOutboundMessagesToInternalBus = true;
            _routingConfiguration.Add<TestEventOne>(new StaticOutboundRouter(new TestProducerEndpoint("eventOne")),
                typeof(OutboundConnector));

            var messages =
                await _behavior.Handle(new object[] { new TestEventOne(), new TestEventTwo() }, Task.FromResult);

            messages.Count.Should().Be(1);
            messages.First().Should().NotBeOfType<TestEventOne>();
        }

        [Fact]
        public async Task Handle_MessagesWithPublishToInternBusOption_RoutedMessageIsRepublishedWithAutoUnwrap()
        {
            _routingConfiguration.PublishOutboundMessagesToInternalBus = true;
            _routingConfiguration.Add<TestEventOne>(new StaticOutboundRouter(new TestProducerEndpoint("eventOne")),
                typeof(OutboundConnector));

            await _behavior.Handle(new object[] { new TestEventOne(), new TestEventTwo() }, Task.FromResult);

            _testSubscriber.ReceivedMessages.Count.Should().Be(1);
            _testSubscriber.ReceivedMessages.First().Should().BeOfType<TestEventOne>();
        }

        [Fact]
        public async Task Handle_OutboundEnvelopeWithPublishToInternBusOption_OutboundEnvelopeIsNotFiltered()
        {
            _routingConfiguration.PublishOutboundMessagesToInternalBus = true;
            _routingConfiguration.Add<TestEventOne>(new StaticOutboundRouter(new TestProducerEndpoint("eventOne")),
                typeof(OutboundConnector));

            var messages =
                await _behavior.Handle(
                    new object[]
                    {
                        new OutboundEnvelope<TestEventOne>(
                            new TestEventOne(),
                            null,
                            new TestProducerEndpoint("eventOne"))
                    }, Task.FromResult);

            messages.Count.Should().Be(1);
        }

        [Fact]
        // Test for possible issue similar to #33: messages don't have to be registered with HandleMessagesOfType to be relayed
        public async Task Handle_UnhandledMessageType_CorrectlyRelayed()
        {
            var message = new SomeUnhandledMessage { Content = "abc" };
            _routingConfiguration.Add<SomeUnhandledMessage>(
                new StaticOutboundRouter(new TestProducerEndpoint("eventOne")),
                typeof(OutboundConnector));

            await _behavior.Handle(new[] { message }, Task.FromResult);

            _broker.ProducedMessages.Count.Should().Be(1);
        }

        [Fact]
        public async Task Handle_MultipleRoutesToMultipleBrokers_CorrectlyQueued()
        {
            _routingConfiguration
                .Add<TestEventOne>(new StaticOutboundRouter(new TestProducerEndpoint("eventOne")))
                .Add<TestEventTwo>(new StaticOutboundRouter(new TestOtherProducerEndpoint("eventTwo")))
                .Add<TestEventThree>(new StaticOutboundRouter(new TestProducerEndpoint("eventThree")));

            await _behavior.Handle(new[] { new TestEventOne() }, Task.FromResult);
            await _behavior.Handle(new[] { new TestEventThree(), }, Task.FromResult);
            await _behavior.Handle(new[] { new TestEventTwo() }, Task.FromResult);
            await _outboundQueue.Commit();

            var queued = (await _outboundQueue.Dequeue(10)).ToArray();
            queued.Length.Should().Be(3);
            queued[0].Endpoint.Should().BeOfType<TestProducerEndpoint>();
            queued[1].Endpoint.Should().BeOfType<TestProducerEndpoint>();
            queued[2].Endpoint.Should().BeOfType<TestOtherProducerEndpoint>();
        }

        [Fact]
        public async Task Handle_MultipleRoutesToMultipleBrokers_CorrectlyRelayed()
        {
            _routingConfiguration
                .Add<TestEventOne>(new StaticOutboundRouter(new TestProducerEndpoint("eventOne")),
                    typeof(OutboundConnector))
                .Add<TestEventTwo>(new StaticOutboundRouter(new TestOtherProducerEndpoint("eventTwo")),
                    typeof(OutboundConnector))
                .Add<TestEventThree>(new StaticOutboundRouter(new TestProducerEndpoint("eventThree")),
                    typeof(OutboundConnector));

            await _behavior.Handle(new[] { new TestEventOne() }, Task.FromResult);
            await _behavior.Handle(new[] { new TestEventThree(), }, Task.FromResult);
            await _behavior.Handle(new[] { new TestEventTwo() }, Task.FromResult);

            _broker.ProducedMessages.Count.Should().Be(2);
            _otherBroker.ProducedMessages.Count.Should().Be(1);
        }
    }
}