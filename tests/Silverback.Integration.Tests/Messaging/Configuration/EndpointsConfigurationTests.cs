// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Configuration;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public class EndpointsConfigurationTests
{
    [Fact]
    public async Task AddOutbound_MultipleEndpoints_MessagesCorrectlyRouted()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
                .AddEndpoints(
                    endpoints =>
                        endpoints
                            .AddOutbound<TestEventOne>(new TestProducerConfiguration("test1"))
                            .AddOutbound<IIntegrationEvent>(new TestProducerConfiguration("test2"))));

        TestBroker broker = serviceProvider.GetRequiredService<TestBroker>();
        await broker.ConnectAsync();

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        // -> to both endpoints
        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        // -> to test2
        await publisher.PublishAsync(new TestEventTwo());
        await publisher.PublishAsync(new TestEventTwo());
        await publisher.PublishAsync(new TestEventTwo());

        // -> to nowhere
        await publisher.PublishAsync(new TestInternalEventOne());

        broker.ProducedMessages.Should().HaveCount(7);
        broker.ProducedMessages.Count(x => x.Endpoint.RawName == "test1").Should().Be(2);
        broker.ProducedMessages.Count(x => x.Endpoint.RawName == "test2").Should().Be(5);
    }

    [Fact]
    public async Task AddOutbound_WithMultipleBrokers_MessagesCorrectlyRouted()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>()
                        .AddBroker<TestOtherBroker>())
                .AddEndpoints(
                    endpoints =>
                        endpoints
                            .AddOutbound<TestEventOne>(new TestProducerConfiguration("test1"))
                            .AddOutbound<TestEventTwo>(new TestOtherProducerConfiguration("test2"))));

        TestBroker broker = serviceProvider.GetRequiredService<TestBroker>();
        await broker.ConnectAsync();

        TestOtherBroker otherBroker = serviceProvider.GetRequiredService<TestOtherBroker>();
        await otherBroker.ConnectAsync();

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        await publisher.PublishAsync(new TestEventTwo());
        await publisher.PublishAsync(new TestEventTwo());
        await publisher.PublishAsync(new TestEventTwo());

        broker.ProducedMessages.Should().HaveCount(2);
        otherBroker.ProducedMessages.Should().HaveCount(3);
    }

    [Fact]
    public async Task AddInbound_MultipleEndpoints_ConsumersCorrectlyConnected()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>())
                .AddEndpoints(
                    endpoints =>
                        endpoints
                            .AddInbound(new TestConsumerConfiguration("test1"))
                            .AddInbound(new TestConsumerConfiguration("test2"))));

        TestBroker broker = serviceProvider.GetRequiredService<TestBroker>();
        await broker.ConnectAsync();

        broker.Consumers.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddInbound_WithMultipleBrokers_ConsumersCorrectlyConnected()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>()
                        .AddBroker<TestOtherBroker>())
                .AddEndpoints(
                    endpoints =>
                        endpoints
                            .AddInbound(new TestConsumerConfiguration("test1"))
                            .AddInbound(new TestOtherConsumerConfiguration("test2"))));

        TestBroker broker = serviceProvider.GetRequiredService<TestBroker>();
        await broker.ConnectAsync();

        TestOtherBroker otherBroker = serviceProvider.GetRequiredService<TestOtherBroker>();
        await otherBroker.ConnectAsync();

        broker.Consumers.Should().HaveCount(1);
        otherBroker.Consumers.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddOutbound_WithIdempotentRegistration_RegisterOnlyOnce()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
                .AddEndpoints(
                    endpoints =>
                        endpoints
                            .AddOutbound<TestEventOne>(new TestProducerConfiguration("test1"))
                            .AddOutbound<TestEventOne>(new TestProducerConfiguration("test1"))));

        TestBroker broker = serviceProvider.GetRequiredService<TestBroker>();
        await broker.ConnectAsync();

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(new TestEventOne());

        broker.ProducedMessages.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddOutbound_WithIdempotentRegistration_RegisterMultipleTimesWhenAllowed()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>()
                        .AllowDuplicateEndpointRegistrations())
                .AddEndpoints(
                    endpoints =>
                        endpoints
                            .AddOutbound<TestEventOne>(new TestProducerConfiguration("test1"))
                            .AddOutbound<TestEventOne>(new TestProducerConfiguration("test1"))));

        TestBroker broker = serviceProvider.GetRequiredService<TestBroker>();
        await broker.ConnectAsync();

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(new TestEventOne());

        broker.ProducedMessages.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddOutbound_WithIdempotentRegistration_LogCriticalWhenConfigurationDiffers()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddLoggerSubstitute()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>())
                .AddEndpoints(
                    endpoints =>
                        endpoints
                            .AddOutbound<TestEventOne>(new TestProducerConfiguration("test1"))
                            .AddOutbound<TestEventOne>(
                                new TestProducerConfiguration("test1")
                                {
                                    Chunk = new ChunkSettings { AlwaysAddHeaders = true, Size = 200 }
                                })));

        TestBroker broker = serviceProvider.GetRequiredService<TestBroker>();
        LoggerSubstitute<EndpointsConfigurationBuilder> loggerSubstitute =
            (LoggerSubstitute<EndpointsConfigurationBuilder>)serviceProvider
                .GetRequiredService<ILogger<EndpointsConfigurationBuilder>>();

        await broker.ConnectAsync();

        loggerSubstitute.Received(
            LogLevel.Critical,
            typeof(EndpointConfigurationException),
            null,
            1102,
            "An endpoint \"test1\" for message type \"Silverback.Tests.Types.Domain.TestEventOne\" is already registered with a different configuration. If this is intentional you can disable this check using the AllowDuplicateEndpointRegistrations.");
    }

    [Fact]
    public async Task AddInbound_WithInvalidEndpoint_ValidEndpointsAdded()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
                .AddEndpoints(
                    endpoints =>
                        endpoints
                            .AddInbound(new TestConsumerConfiguration("test1"))
                            .AddInbound(new TestConsumerConfiguration(string.Empty))
                            .AddInbound(new TestConsumerConfiguration("test2"))));

        TestBroker broker = serviceProvider.GetRequiredService<TestBroker>();
        await broker.ConnectAsync();

        broker.Consumers.Should().HaveCount(2);
        broker.Consumers[0].Configuration.RawName.Should().Be("test1");
        broker.Consumers[1].Configuration.RawName.Should().Be("test2");
    }

    [Fact]
    public async Task AddInboundOutbound_MultipleConfiguratorsThrowing_OtherEndpointsAdded()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
                .AddEndpoints(
                    endpoints =>
                        endpoints
                            .AddOutbound<TestEventOne>(new TestProducerConfiguration("test1"))
                            .AddInbound(new TestConsumerConfiguration("test1")))
                .AddEndpoints(_ => throw new InvalidOperationException())
                .AddEndpoints(
                    endpoints =>
                        endpoints
                            .AddOutbound<TestEventOne>(new TestProducerConfiguration("test2"))
                            .AddInbound(new TestConsumerConfiguration("test2"))));

        TestBroker broker = serviceProvider.GetRequiredService<TestBroker>();
        await broker.ConnectAsync();

        broker.Producers.Should().HaveCount(2);
        broker.Producers[0].Configuration.RawName.Should().Be("test1");
        broker.Producers[1].Configuration.RawName.Should().Be("test2");
        broker.Consumers.Should().HaveCount(2);
        broker.Consumers[0].Configuration.RawName.Should().Be("test1");
        broker.Consumers[1].Configuration.RawName.Should().Be("test2");
    }
}
