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
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Old.Broker
{
    public class OutboundRoutingTests : KafkaTestFixture
    {
        public OutboundRoutingTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact(Skip = "Deprecated")]
        public async Task StaticSingleEndpoint_RoutedCorrectly()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<TestEventOne>(new KafkaProducerEndpoint("test-e2e-one"))
                                .AddOutbound<TestEventTwo>(new KafkaProducerEndpoint("test-e2e-two"))
                                .AddOutbound<TestEventThree>(new KafkaProducerEndpoint("test-e2e-three")))
                        .AddSingletonOutboundRouter<TestPrioritizedOutboundRouter>()
                        .AddIntegrationSpy())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventTwo());
            await publisher.PublishAsync(new TestEventThree());
            await publisher.PublishAsync(new TestEventFour());

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
            Helper.Spy.OutboundEnvelopes[0].Endpoint.Name.Should().Be("test-e2e-one");
            Helper.Spy.OutboundEnvelopes[1].Endpoint.Name.Should().Be("test-e2e-two");
            Helper.Spy.OutboundEnvelopes[2].Endpoint.Name.Should().Be("test-e2e-three");
        }

        [Fact(Skip = "Deprecated")]
        public async Task StaticBroadcast_RoutedCorrectly()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<TestEventOne>(
                                    new KafkaProducerEndpoint("test-e2e-one"),
                                    new KafkaProducerEndpoint("test-e2e-two"),
                                    new KafkaProducerEndpoint("test-e2e-three")))
                        .AddIntegrationSpy())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventTwo());
            await publisher.PublishAsync(new TestEventThree());

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
            Helper.Spy.OutboundEnvelopes[0].Endpoint.Name.Should().Be("test-e2e-one");
            Helper.Spy.OutboundEnvelopes[1].Endpoint.Name.Should().Be("test-e2e-two");
            Helper.Spy.OutboundEnvelopes[2].Endpoint.Name.Should().Be("test-e2e-three");
        }

        [Fact(Skip = "Deprecated")]
        public async Task DynamicCustomRouting_RoutedCorrectly()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<TestPrioritizedCommand, TestPrioritizedOutboundRouter>())
                        .AddSingletonOutboundRouter<TestPrioritizedOutboundRouter>()
                        .AddIntegrationSpy())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<ICommandPublisher>();
            await publisher.ExecuteAsync(new TestPrioritizedCommand { Priority = Priority.Low });
            await publisher.ExecuteAsync(new TestPrioritizedCommand { Priority = Priority.Low });
            await publisher.ExecuteAsync(new TestPrioritizedCommand { Priority = Priority.High });

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(6);
            Helper.Spy.OutboundEnvelopes.Count(envelope => envelope.Endpoint.Name == "test-e2e-all")
                .Should().Be(3);
            Helper.Spy.OutboundEnvelopes.Count(envelope => envelope.Endpoint.Name == "test-e2e-low")
                .Should().Be(2);
            Helper.Spy.OutboundEnvelopes.Count(envelope => envelope.Endpoint.Name == "test-e2e-normal")
                .Should().Be(0);
            Helper.Spy.OutboundEnvelopes.Count(envelope => envelope.Endpoint.Name == "test-e2e-high")
                .Should().Be(1);
        }
    }
}
