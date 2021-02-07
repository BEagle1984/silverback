// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration
{
    public class EndpointsConfigurationTests
    {
        [Fact]
        public async Task AddOutbound_MultipleEndpoints_MessagesCorrectlyRouted()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
                    .AddEndpoints(
                        endpoints =>
                            endpoints
                                .AddOutbound<TestEventOne>(new TestProducerEndpoint("test1"))
                                .AddOutbound<IIntegrationEvent>(new TestProducerEndpoint("test2"))));

            var broker = serviceProvider.GetRequiredService<TestBroker>();
            await broker.ConnectAsync();

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

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
            broker.ProducedMessages.Count(x => x.Endpoint.Name == "test1").Should().Be(2);
            broker.ProducedMessages.Count(x => x.Endpoint.Name == "test2").Should().Be(5);
        }

        [Fact]
        public async Task AddOutbound_WithMultipleBrokers_MessagesCorrectlyRouted()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
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
                                .AddOutbound<TestEventOne>(new TestProducerEndpoint("test1"))
                                .AddOutbound<TestEventTwo>(new TestOtherProducerEndpoint("test2"))));

            var broker = serviceProvider.GetRequiredService<TestBroker>();
            await broker.ConnectAsync();

            var otherBroker = serviceProvider.GetRequiredService<TestOtherBroker>();
            await otherBroker.ConnectAsync();

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

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
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddBroker<TestBroker>())
                    .AddEndpoints(
                        endpoints =>
                            endpoints
                                .AddInbound(new TestConsumerEndpoint("test1"))
                                .AddInbound(new TestConsumerEndpoint("test2"))));

            var broker = serviceProvider.GetRequiredService<TestBroker>();
            await broker.ConnectAsync();

            broker.Consumers.Should().HaveCount(2);
        }

        [Fact]
        public async Task AddInbound_WithMultipleBrokers_ConsumersCorrectlyConnected()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
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
                                .AddInbound(new TestConsumerEndpoint("test1"))
                                .AddInbound(new TestOtherConsumerEndpoint("test2"))));

            var broker = serviceProvider.GetRequiredService<TestBroker>();
            await broker.ConnectAsync();

            var otherBroker = serviceProvider.GetRequiredService<TestOtherBroker>();
            await otherBroker.ConnectAsync();

            broker.Consumers.Should().HaveCount(1);
            otherBroker.Consumers.Should().HaveCount(1);
        }

        [Fact]
        public async Task AddOutbound_WithIdempotentRegistration_RegisterOnlyOnce()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
                    .AddEndpoints(
                        endpoints =>
                            endpoints
                                .AddOutbound<TestEventOne>(new TestProducerEndpoint("test1"))
                                .AddOutbound<TestEventOne>(new TestProducerEndpoint("test1"))));

            var broker = serviceProvider.GetRequiredService<TestBroker>();
            await broker.ConnectAsync();

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(new TestEventOne());

            broker.ProducedMessages.Should().HaveCount(1);
        }

        [Fact]
        public async Task AddOutbound_WithIdempotentRegistration_RegisterMultipleTimesWhenAllowed()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
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
                                .AddOutbound<TestEventOne>(new TestProducerEndpoint("test1"))
                                .AddOutbound<TestEventOne>(new TestProducerEndpoint("test1"))));

            var broker = serviceProvider.GetRequiredService<TestBroker>();
            await broker.ConnectAsync();

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(new TestEventOne());

            broker.ProducedMessages.Should().HaveCount(2);
        }

        [Fact]
        public async Task AddOutbound_WithIdempotentRegistration_LogCriticalWhenConfigurationDiffers()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddLoggerSubstitute()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddBroker<TestBroker>())
                    .AddEndpoints(
                        endpoints =>
                            endpoints
                                .AddOutbound<TestEventOne>(new TestProducerEndpoint("test1"))
                                .AddOutbound<TestEventOne>(
                                    new TestProducerEndpoint("test1")
                                    {
                                        Chunk = new ChunkSettings { AlwaysAddHeaders = true, Size = 200 }
                                    })));

            var broker = serviceProvider.GetRequiredService<TestBroker>();
            var loggerSubstitute =
                (LoggerSubstitute<EndpointsConfigurationBuilder>)serviceProvider
                    .GetRequiredService<ILogger<EndpointsConfigurationBuilder>>();

            await broker.ConnectAsync();

            loggerSubstitute.Received(
                LogLevel.Critical,
                typeof(EndpointConfigurationException),
                null,
                1102,
                "An endpoint 'test1' for message type 'Silverback.Tests.Types.Domain.TestEventOne' but with a different configuration is already registered.");
        }

        [Fact]
        public async Task AddInbound_WithInvalidEndpoint_ValidEndpointsAdded()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
                    .AddEndpoints(
                        endpoints =>
                            endpoints
                                .AddInbound(new TestConsumerEndpoint("test1"))
                                .AddInbound(new TestConsumerEndpoint(string.Empty))
                                .AddInbound(new TestConsumerEndpoint("test2"))));

            var broker = serviceProvider.GetRequiredService<TestBroker>();
            await broker.ConnectAsync();

            broker.Consumers.Should().HaveCount(2);
            broker.Consumers[0].Endpoint.Name.Should().Be("test1");
            broker.Consumers[1].Endpoint.Name.Should().Be("test2");
        }

        [Fact]
        public async Task AddOutbound_WithInvalidEndpoint_ValidEndpointsAdded()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
                    .AddEndpoints(
                        endpoints =>
                            endpoints
                                .AddOutbound<TestEventOne>(new TestProducerEndpoint("test1"))
                                .AddOutbound<TestEventOne>(new TestProducerEndpoint(string.Empty))
                                .AddOutbound<TestEventOne>(new TestProducerEndpoint("test2"))));

            var broker = serviceProvider.GetRequiredService<TestBroker>();
            await broker.ConnectAsync();

            broker.Producers.Should().HaveCount(2);
            broker.Producers[0].Endpoint.Name.Should().Be("test1");
            broker.Producers[1].Endpoint.Name.Should().Be("test2");
        }

        [Fact]
        public async Task AddInboundOutbound_MultipleConfiguratorsWithInvalidEndpoints_ValidEndpointsAdded()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
                    .AddEndpoints(
                        endpoints =>
                            endpoints
                                .AddOutbound<TestEventOne>(new TestProducerEndpoint("test1"))
                                .AddInbound(new TestConsumerEndpoint(string.Empty))
                                .AddInbound(new TestConsumerEndpoint("test1")))
                    .AddEndpoints(_ => throw new InvalidOperationException())
                    .AddEndpoints(
                        endpoints =>
                            endpoints
                                .AddOutbound<TestEventOne>(new TestProducerEndpoint(string.Empty))
                                .AddOutbound<TestEventOne>(new TestProducerEndpoint("test2"))
                                .AddInbound(new TestConsumerEndpoint("test2"))));

            var broker = serviceProvider.GetRequiredService<TestBroker>();
            await broker.ConnectAsync();

            broker.Producers.Should().HaveCount(2);
            broker.Producers[0].Endpoint.Name.Should().Be("test1");
            broker.Producers[1].Endpoint.Name.Should().Be("test2");
            broker.Consumers.Should().HaveCount(2);
            broker.Consumers[0].Endpoint.Name.Should().Be("test1");
            broker.Consumers[1].Endpoint.Name.Should().Be("test2");
        }
    }
}
