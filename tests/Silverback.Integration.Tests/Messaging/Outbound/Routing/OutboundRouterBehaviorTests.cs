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
using Silverback.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Outbound.Routing;

public class OutboundRouterBehaviorTests
{
    private readonly OutboundRouterBehavior _behavior;

    private readonly IOutboundRoutingConfiguration _routingConfiguration;

    private readonly TestBroker _broker;

    private readonly TestOtherBroker _otherBroker;

    private readonly TestSubscriber _testSubscriber;

    public OutboundRouterBehaviorTests()
    {
        ServiceCollection services = new();

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

        IServiceProvider serviceProvider = services.BuildServiceProvider();

        _behavior = (OutboundRouterBehavior)serviceProvider.GetServices<IBehavior>()
            .First(s => s is OutboundRouterBehavior);
        _routingConfiguration =
            (OutboundRoutingConfiguration)serviceProvider
                .GetRequiredService<IOutboundRoutingConfiguration>();
        _broker = serviceProvider.GetRequiredService<TestBroker>();
        _otherBroker = serviceProvider.GetRequiredService<TestOtherBroker>();
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
        _routingConfiguration.AddRoute(typeof(IIntegrationMessage), new TestProducerConfiguration("allMessages"));
        _routingConfiguration.AddRoute(typeof(IIntegrationEvent), new TestProducerConfiguration("allEvents"));
        _routingConfiguration.AddRoute(typeof(TestEventOne), new TestProducerConfiguration("eventOne"));
        _routingConfiguration.AddRoute(typeof(TestEventTwo), new TestProducerConfiguration("eventTwo"));

        await _behavior.HandleAsync(
            message,
            nextMessage => Task.FromResult(new[] { nextMessage }.AsReadOnlyCollection())!);

        foreach (string expectedEndpointName in expectedEndpointNames)
        {
            _broker.ProducedMessages.Count(producedMessage => producedMessage.Endpoint.RawName == expectedEndpointName)
                .Should().Be(1);
        }

        IEnumerable<string> notExpectedEndpointNames = _routingConfiguration
            .Routes.Select(route => route.ProducerConfiguration.RawName)
            .Where(endpointName => !expectedEndpointNames.Contains(endpointName));

        foreach (string notExpectedEndpointName in notExpectedEndpointNames)
        {
            _broker.ProducedMessages.Count(producedMessage => producedMessage.Endpoint.RawName == notExpectedEndpointName)
                .Should().Be(0);
        }
    }

    [Fact]
    public async Task HandleAsync_Message_CorrectlyRouted()
    {
        _routingConfiguration.AddRoute(typeof(TestEventOne), new TestProducerConfiguration("eventOne"));

        await _behavior.HandleAsync(
            new TestEventOne(),
            nextMessage => Task.FromResult(new[] { nextMessage }.AsReadOnlyCollection())!);

        _broker.ProducedMessages.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_Message_RoutedMessageIsFiltered()
    {
        _routingConfiguration.AddRoute(typeof(TestEventOne), new TestProducerConfiguration("eventOne"));

        IReadOnlyCollection<object?> messages = await _behavior.HandleAsync(
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
        _routingConfiguration.AddRoute(typeof(TestEventOne), new TestProducerConfiguration("eventOne"));

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
        _routingConfiguration.AddRoute(typeof(TestEventOne), new TestProducerConfiguration("eventOne"));

        IReadOnlyCollection<object?> messages = await _behavior.HandleAsync(
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
        _routingConfiguration.AddRoute(typeof(TestEventOne), new TestProducerConfiguration("eventOne"));

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
        _routingConfiguration.AddRoute(typeof(TestEventOne), new TestProducerConfiguration("eventOne"));

        IReadOnlyCollection<object?> messages = await _behavior.HandleAsync(
            new OutboundEnvelope<TestEventOne>(
                new TestEventOne(),
                null,
                new TestProducerConfiguration("eventOne").GetDefaultEndpoint()),
            nextMessage => Task.FromResult(new[] { nextMessage }.AsReadOnlyCollection())!);

        messages.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_UnhandledMessageType_CorrectlyRelayed()
    {
        /* Test for possible issue similar to #33: messages don't have to be registered with HandleMessagesOfType
         * to be relayed */

        SomeUnhandledMessage message = new() { Content = "abc" };
        _routingConfiguration.AddRoute(typeof(SomeUnhandledMessage), new TestProducerConfiguration("eventOne"));

        await _behavior.HandleAsync(
            message,
            nextMessage => Task.FromResult(new[] { nextMessage }.AsReadOnlyCollection())!);

        _broker.ProducedMessages.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_MultipleRoutesToMultipleBrokers_CorrectlyRelayed()
    {
        _routingConfiguration.AddRoute(typeof(TestEventOne), new TestProducerConfiguration("eventOne"));
        _routingConfiguration.AddRoute(typeof(TestEventTwo), new TestOtherProducerConfiguration("eventTwo"));
        _routingConfiguration.AddRoute(typeof(TestEventThree), new TestProducerConfiguration("eventThree"));

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
