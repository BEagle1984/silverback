// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Outbound.Routing
{
    public class OutboundRouterBehaviorTests
    {
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

            services
                .AddSingleton(Substitute.For<IHostApplicationLifetime>())
                .AddLoggerSubstitute();

            _serviceProvider = services.BuildServiceProvider();

            _behavior = (OutboundRouterBehavior)_serviceProvider.GetServices<IBehavior>()
                .First(s => s is OutboundRouterBehavior);
            _routingConfiguration =
                (OutboundRoutingConfiguration)_serviceProvider
                    .GetRequiredService<IOutboundRoutingConfiguration>();
            _broker = _serviceProvider.GetRequiredService<TestBroker>();
            _otherBroker = _serviceProvider.GetRequiredService<TestOtherBroker>();
        }

        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "TestData")]
        public static IEnumerable<object[]> HandleAsync_Message_CorrectlyRoutedToEndpoints_TestData =>
            new[]
            {
                new object[] { new TestEventOne(), new[] { "allMessages", "allEvents", "eventOne" } },
                new object[] { new TestEventTwo(), new[] { "allMessages", "allEvents", "eventTwo" } }
            };

        [Theory]
        [MemberData(nameof(HandleAsync_Message_CorrectlyRoutedToEndpoints_TestData))]
        public async Task HandleAsync_Message_CorrectlyRoutedToStaticEndpoint(
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

            await _behavior.HandleAsync(
                message,
                nextMessage => Task.FromResult(new[] { nextMessage }.AsReadOnlyCollection())!);

            foreach (var expectedEndpointName in expectedEndpointNames)
            {
                _broker.ProducedMessages.Count(envelope => envelope.Endpoint.Name == expectedEndpointName)
                    .Should()
                    .Be(1);
            }

            var notExpectedEndpointNames = _routingConfiguration
                .Routes.Select(route => route.GetOutboundRouter(_serviceProvider).Endpoints.First().Name)
                .Where(r => !expectedEndpointNames.Contains(r));

            foreach (var notExpectedEndpointName in notExpectedEndpointNames)
            {
                _broker.ProducedMessages.Count(envelope => envelope.Endpoint.Name == notExpectedEndpointName)
                    .Should()
                    .Be(0);
            }
        }

        [Fact]
        public async Task HandleAsync_Message_CorrectlyRouted()
        {
            _routingConfiguration.Add<TestEventOne>(
                _ => new StaticOutboundRouter(new TestProducerEndpoint("eventOne")));

            await _behavior.HandleAsync(
                new TestEventOne(),
                nextMessage => Task.FromResult(new[] { nextMessage }.AsReadOnlyCollection())!);

            _broker.ProducedMessages.Should().HaveCount(1);
        }

        [Fact]
        public async Task HandleAsync_Message_RoutedMessageIsFiltered()
        {
            _routingConfiguration.Add<TestEventOne>(
                _ => new StaticOutboundRouter(new TestProducerEndpoint("eventOne")));

            var messages = await _behavior.HandleAsync(
                new TestEventOne(),
                nextMessage => Task.FromResult(new[] { nextMessage }.AsReadOnlyCollection())!);
            messages.Should().BeEmpty();

            messages = await _behavior.HandleAsync(
                new TestEventTwo(),
                nextMessage => Task.FromResult(new[] { nextMessage }.AsReadOnlyCollection())!);
            messages.Should().NotBeEmpty();
        }

        [Fact]
        public async Task HandleAsync_Message_RoutedMessageIsRepublishedWithoutAutoUnwrap()
        {
            _routingConfiguration.Add<TestEventOne>(
                _ => new StaticOutboundRouter(new TestProducerEndpoint("eventOne")));

            await _behavior.HandleAsync(
                new TestEventOne(),
                nextMessage => Task.FromResult(new[] { nextMessage }.AsReadOnlyCollection())!);

            _testSubscriber.ReceivedMessages.Should()
                .BeEmpty(); // Because TestSubscriber discards the envelopes
        }

        [Fact]
        public async Task HandleAsync_MessagesWithPublishToInternBusOption_RoutedMessageIsFiltered()
        {
            _routingConfiguration.PublishOutboundMessagesToInternalBus = true;
            _routingConfiguration.Add<TestEventOne>(
                _ => new StaticOutboundRouter(new TestProducerEndpoint("eventOne")));

            var messages = await _behavior.HandleAsync(
                new TestEventOne(),
                nextMessage => Task.FromResult(new[] { nextMessage }.AsReadOnlyCollection())!);
            messages.Should().BeEmpty();

            messages = await _behavior.HandleAsync(
                new TestEventTwo(),
                nextMessage => Task.FromResult(new[] { nextMessage }.AsReadOnlyCollection())!);
            messages.Should().NotBeEmpty();
        }

        [Fact]
        public async Task
            HandleAsync_MessagesWithPublishToInternBusOption_RoutedMessageIsRepublishedWithAutoUnwrap()
        {
            _routingConfiguration.PublishOutboundMessagesToInternalBus = true;
            _routingConfiguration.Add<TestEventOne>(
                _ => new StaticOutboundRouter(new TestProducerEndpoint("eventOne")));

            await _behavior.HandleAsync(
                new TestEventOne(),
                nextMessage => Task.FromResult(new[] { nextMessage }.AsReadOnlyCollection())!);

            _testSubscriber.ReceivedMessages.Should().HaveCount(1);
            _testSubscriber.ReceivedMessages.First().Should().BeOfType<TestEventOne>();
        }

        [Fact]
        public async Task HandleAsync_EnvelopeWithPublishToInternBusOption_OutboundEnvelopeIsNotFiltered()
        {
            _routingConfiguration.PublishOutboundMessagesToInternalBus = true;
            _routingConfiguration.Add<TestEventOne>(
                _ => new StaticOutboundRouter(new TestProducerEndpoint("eventOne")));

            var messages = await _behavior.HandleAsync(
                new OutboundEnvelope<TestEventOne>(
                    new TestEventOne(),
                    null,
                    new TestProducerEndpoint("eventOne")),
                nextMessage => Task.FromResult(new[] { nextMessage }.AsReadOnlyCollection())!);

            messages.Should().HaveCount(1);
        }

        [Fact]
        public async Task HandleAsync_UnhandledMessageType_CorrectlyRelayed()
        {
            /* Test for possible issue similar to #33: messages don't have to be registered with HandleMessagesOfType
             * to be relayed */

            var message = new SomeUnhandledMessage { Content = "abc" };
            _routingConfiguration.Add<SomeUnhandledMessage>(
                _ => new StaticOutboundRouter(new TestProducerEndpoint("eventOne")));

            await _behavior.HandleAsync(
                message,
                nextMessage => Task.FromResult(new[] { nextMessage }.AsReadOnlyCollection())!);

            _broker.ProducedMessages.Should().HaveCount(1);
        }

        [Fact]
        public async Task HandleAsync_MultipleRoutesToMultipleBrokers_CorrectlyRelayed()
        {
            _routingConfiguration
                .Add<TestEventOne>(_ => new StaticOutboundRouter(new TestProducerEndpoint("eventOne")))
                .Add<TestEventTwo>(_ => new StaticOutboundRouter(new TestOtherProducerEndpoint("eventTwo")))
                .Add<TestEventThree>(_ => new StaticOutboundRouter(new TestProducerEndpoint("eventThree")));

            await _behavior.HandleAsync(
                new TestEventOne(),
                nextMessage => Task.FromResult(new[] { nextMessage }.AsReadOnlyCollection())!);
            await _behavior.HandleAsync(
                new TestEventThree(),
                nextMessage => Task.FromResult(new[] { nextMessage }.AsReadOnlyCollection())!);
            await _behavior.HandleAsync(
                new TestEventTwo(),
                nextMessage => Task.FromResult(new[] { nextMessage }.AsReadOnlyCollection())!);

            _broker.ProducedMessages.Should().HaveCount(2);
            _otherBroker.ProducedMessages.Should().HaveCount(1);
        }
    }
}
