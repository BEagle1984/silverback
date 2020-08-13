// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Broker
{
    [Trait("Category", "E2E")]
    public class OutboundRoutingTests : E2ETestFixture
    {
        [Fact]
        public async Task StaticSingleEndpoint_RoutedCorrectly()
        {
            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddInMemoryBroker()
                                .AddInMemoryChunkStore())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<TestEventOne>(new KafkaProducerEndpoint("test-e2e-one"))
                                .AddOutbound<TestEventTwo>(new KafkaProducerEndpoint("test-e2e-two"))
                                .AddOutbound<TestEventThree>(new KafkaProducerEndpoint("test-e2e-three")))
                        .AddSingletonOutboundRouter<TestPrioritizedOutboundRouter>()
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventTwo());
            await publisher.PublishAsync(new TestEventThree());
            await publisher.PublishAsync(new TestEventFour());

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(3);
            SpyBehavior.OutboundEnvelopes[0].Endpoint.Name.Should().Be("test-e2e-one");
            SpyBehavior.OutboundEnvelopes[1].Endpoint.Name.Should().Be("test-e2e-two");
            SpyBehavior.OutboundEnvelopes[2].Endpoint.Name.Should().Be("test-e2e-three");
        }

        [Fact]
        public async Task StaticBroadcast_RoutedCorrectly()
        {
            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddInMemoryBroker()
                                .AddInMemoryChunkStore())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<TestEventOne>(
                                    new KafkaProducerEndpoint("test-e2e-one"),
                                    new KafkaProducerEndpoint("test-e2e-two"),
                                    new KafkaProducerEndpoint("test-e2e-three")))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventTwo());
            await publisher.PublishAsync(new TestEventThree());

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(3);
            SpyBehavior.OutboundEnvelopes[0].Endpoint.Name.Should().Be("test-e2e-one");
            SpyBehavior.OutboundEnvelopes[1].Endpoint.Name.Should().Be("test-e2e-two");
            SpyBehavior.OutboundEnvelopes[2].Endpoint.Name.Should().Be("test-e2e-three");
        }

        [Fact]
        public async Task DynamicCustomRouting_RoutedCorrectly()
        {
            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddInMemoryBroker()
                                .AddInMemoryChunkStore())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<TestPrioritizedCommand, TestPrioritizedOutboundRouter>())
                        .AddSingletonOutboundRouter<TestPrioritizedOutboundRouter>()
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<ICommandPublisher>();
            await publisher.ExecuteAsync(new TestPrioritizedCommand { Priority = Priority.Low });
            await publisher.ExecuteAsync(new TestPrioritizedCommand { Priority = Priority.Low });
            await publisher.ExecuteAsync(new TestPrioritizedCommand { Priority = Priority.High });

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(6);
            SpyBehavior.OutboundEnvelopes.Count(envelope => envelope.Endpoint.Name == "test-e2e-all")
                .Should().Be(3);
            SpyBehavior.OutboundEnvelopes.Count(envelope => envelope.Endpoint.Name == "test-e2e-low")
                .Should().Be(2);
            SpyBehavior.OutboundEnvelopes.Count(envelope => envelope.Endpoint.Name == "test-e2e-normal")
                .Should().Be(0);
            SpyBehavior.OutboundEnvelopes.Count(envelope => envelope.Endpoint.Name == "test-e2e-high")
                .Should().Be(1);
        }
    }
}
