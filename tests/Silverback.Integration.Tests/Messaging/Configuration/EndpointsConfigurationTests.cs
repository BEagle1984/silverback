// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Tests.Integration.TestTypes;
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
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options
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
        public void AddOutbound_WithIdempotentRegistration_ThrowWhenConfigurationDiffers()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
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
            Func<Task> action = async () => await broker.ConnectAsync();

            action.Should().Throw<EndpointConfigurationException>()
                .WithMessage("An endpoint 'test1' for message type 'Silverback.Tests.Types.Domain.TestEventOne' but with a different configuration is already registered.");
        }
    }
}
