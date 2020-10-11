﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Outbound.Routing
{
    public class OutboundRouterBehaviorTests
    {
        // TODO: Still needed? Replace with E2E?

        private readonly OutboundRouterBehavior _behavior;

        private readonly IOutboundRoutingConfiguration _routingConfiguration;

        private readonly TestBroker _broker;

        private readonly TestOtherBroker _otherBroker;

        private readonly TestSubscriber _testSubscriber;

        private readonly IServiceProvider _serviceProvider;

        public OutboundRouterBehaviorTests()
        {
            var services = new ServiceCollection();

            _testSubscriber = new TestSubscriber();

            services.AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>()
                        .AddBroker<TestOtherBroker>())
                .AddSingletonSubscriber(_testSubscriber);

            services.AddNullLogger();

            _serviceProvider = services.BuildServiceProvider();

            _behavior = (OutboundRouterBehavior)_serviceProvider.GetServices<IBehavior>()
                .First(s => s is OutboundRouterBehavior);
            _routingConfiguration =
                (OutboundRoutingConfiguration)_serviceProvider.GetRequiredService<IOutboundRoutingConfiguration>();
            _broker = _serviceProvider.GetRequiredService<TestBroker>();
            _otherBroker = _serviceProvider.GetRequiredService<TestOtherBroker>();
        }

        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "TestData")]
        public static IEnumerable<object[]> Handle_MultipleMessages_CorrectlyRoutedToEndpoints_TestData =>
            new[]
            {
                new object[] { new TestEventOne(), new[] { "allMessages", "allEvents", "eventOne" } },
                new object[] { new TestEventTwo(), new[] { "allMessages", "allEvents", "eventTwo" } }
            };

        [Theory]
        [MemberData(nameof(Handle_MultipleMessages_CorrectlyRoutedToEndpoints_TestData))]
        public async Task Handle_MultipleMessages_CorrectlyRoutedToStaticEndpoint(
            IIntegrationMessage message,
            string[] expectedEndpointNames)
        {
            _routingConfiguration.Add<IIntegrationMessage>(
                _ => new StaticOutboundRouter(new TestProducerEndpoint("allMessages")));
            _routingConfiguration.Add<IIntegrationEvent>(
                _ => new StaticOutboundRouter(new TestProducerEndpoint("allEvents")));
            _routingConfiguration.Add<TestEventOne>(
                _ => new StaticOutboundRouter(new TestProducerEndpoint("eventOne")));
            _routingConfiguration.Add<TestEventTwo>(
                _ => new StaticOutboundRouter(new TestProducerEndpoint("eventTwo")));

            await _behavior.Handle(new[] { message }, Task.FromResult!);

            foreach (var expectedEndpointName in expectedEndpointNames)
            {
                _broker.ProducedMessages.Count(envelope => envelope.Endpoint.Name == expectedEndpointName).Should()
                    .Be(1);
            }

            var notExpectedEndpointNames = _routingConfiguration
                .Routes.Select(route => route.GetOutboundRouter(_serviceProvider).Endpoints.First().Name)
                .Where(r => !expectedEndpointNames.Contains(r));

            foreach (var notExpectedEndpointName in notExpectedEndpointNames)
            {
                _broker.ProducedMessages.Count(envelope => envelope.Endpoint.Name == notExpectedEndpointName).Should()
                    .Be(0);
            }
        }

        [Fact]
        public async Task Handle_Message_CorrectlyRouted()
        {
            _routingConfiguration.Add<TestEventOne>(
                _ => new StaticOutboundRouter(new TestProducerEndpoint("eventOne")));

            await _behavior.Handle(new[] { new TestEventOne() }, Task.FromResult!);

            _broker.ProducedMessages.Count.Should().Be(1);
        }

        [Fact]
        public async Task Handle_Messages_RoutedMessageIsFiltered()
        {
            _routingConfiguration.Add<TestEventOne>(
                _ => new StaticOutboundRouter(new TestProducerEndpoint("eventOne")));

            var messages =
                await _behavior.Handle(new object[] { new TestEventOne(), new TestEventTwo() }, Task.FromResult!);

            messages.Count.Should().Be(1);
            messages.First().Should().NotBeOfType<TestEventOne>();
        }

        [Fact]
        public async Task Handle_Messages_RoutedMessageIsRepublishedWithoutAutoUnwrap()
        {
            _routingConfiguration.Add<TestEventOne>(
                _ => new StaticOutboundRouter(new TestProducerEndpoint("eventOne")));

            await _behavior.Handle(new object[] { new TestEventOne(), new TestEventTwo() }, Task.FromResult!);

            _testSubscriber.ReceivedMessages.Count.Should().Be(0); // Because TestSubscriber discards the envelopes
        }

        [Fact]
        public async Task Handle_MessagesWithPublishToInternBusOption_RoutedMessageIsFiltered()
        {
            _routingConfiguration.PublishOutboundMessagesToInternalBus = true;
            _routingConfiguration.Add<TestEventOne>(
                _ => new StaticOutboundRouter(new TestProducerEndpoint("eventOne")));

            var messages =
                await _behavior.Handle(new object[] { new TestEventOne(), new TestEventTwo() }, Task.FromResult!);

            messages.Count.Should().Be(1);
            messages.First().Should().NotBeOfType<TestEventOne>();
        }

        [Fact]
        public async Task Handle_MessagesWithPublishToInternBusOption_RoutedMessageIsRepublishedWithAutoUnwrap()
        {
            _routingConfiguration.PublishOutboundMessagesToInternalBus = true;
            _routingConfiguration.Add<TestEventOne>(
                _ => new StaticOutboundRouter(new TestProducerEndpoint("eventOne")));

            await _behavior.Handle(new object[] { new TestEventOne(), new TestEventTwo() }, Task.FromResult!);

            _testSubscriber.ReceivedMessages.Count.Should().Be(1);
            _testSubscriber.ReceivedMessages.First().Should().BeOfType<TestEventOne>();
        }

        [Fact]
        public async Task Handle_OutboundEnvelopeWithPublishToInternBusOption_OutboundEnvelopeIsNotFiltered()
        {
            _routingConfiguration.PublishOutboundMessagesToInternalBus = true;
            _routingConfiguration.Add<TestEventOne>(
                _ => new StaticOutboundRouter(new TestProducerEndpoint("eventOne")));

            var messages =
                await _behavior.Handle(
                    new object[]
                    {
                        new OutboundEnvelope<TestEventOne>(
                            new TestEventOne(),
                            null,
                            new TestProducerEndpoint("eventOne"))
                    },
                    Task.FromResult!);

            messages.Count.Should().Be(1);
        }

        [Fact]
        public async Task Handle_UnhandledMessageType_CorrectlyRelayed()
        {
            /* Test for possible issue similar to #33: messages don't have to be registered with HandleMessagesOfType
             * to be relayed */

            var message = new SomeUnhandledMessage { Content = "abc" };
            _routingConfiguration.Add<SomeUnhandledMessage>(
                _ => new StaticOutboundRouter(new TestProducerEndpoint("eventOne")));

            await _behavior.Handle(new[] { message }, Task.FromResult!);

            _broker.ProducedMessages.Count.Should().Be(1);
        }

        [Fact]
        public async Task Handle_MultipleRoutesToMultipleBrokers_CorrectlyRelayed()
        {
            _routingConfiguration
                .Add<TestEventOne>(_ => new StaticOutboundRouter(new TestProducerEndpoint("eventOne")))
                .Add<TestEventTwo>(_ => new StaticOutboundRouter(new TestOtherProducerEndpoint("eventTwo")))
                .Add<TestEventThree>(_ => new StaticOutboundRouter(new TestProducerEndpoint("eventThree")));

            await _behavior.Handle(new[] { new TestEventOne() }, Task.FromResult!);
            await _behavior.Handle(new[] { new TestEventThree(), }, Task.FromResult!);
            await _behavior.Handle(new[] { new TestEventTwo() }, Task.FromResult!);

            _broker.ProducedMessages.Count.Should().Be(2);
            _otherBroker.ProducedMessages.Count.Should().Be(1);
        }
    }
}