// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E
{
    public class OutboundRoutingTests
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly BusConfigurator _configurator;
        private readonly SpyBrokerBehavior _spyBehavior;

        public OutboundRoutingTests()
        {
            var services = new ServiceCollection();

            services
                .AddNullLogger()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options
                    .AddInMemoryBroker()
                    .AddChunkStore<InMemoryChunkStore>()
                    .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                    .AddSingletonOutboundRouter<TestPrioritizedOutboundRouter>())
                .AddSingletonSubscriber<OutboundInboundSubscriber>();

            _serviceProvider = services.BuildServiceProvider();

            _configurator = _serviceProvider.GetRequiredService<BusConfigurator>();
            _spyBehavior = _serviceProvider.GetServices<IBrokerBehavior>().OfType<SpyBrokerBehavior>().First();
        }

        [Fact]
        public async Task E2E_OutboundRouting_StaticSingleEndpoint()
        {
            _configurator.Connect(endpoints => endpoints
                .AddOutbound<TestEventOne>(
                    new KafkaProducerEndpoint("test-e2e-one"))
                .AddOutbound<TestEventTwo>(
                    new KafkaProducerEndpoint("test-e2e-two"))
                .AddOutbound<TestEventThree>(
                    new KafkaProducerEndpoint("test-e2e-three"))
            );

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventTwo());
            await publisher.PublishAsync(new TestEventThree());

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(3);
            _spyBehavior.OutboundEnvelopes[0].Endpoint.Name.Should().Be("test-e2e-one");
            _spyBehavior.OutboundEnvelopes[1].Endpoint.Name.Should().Be("test-e2e-two");
            _spyBehavior.OutboundEnvelopes[2].Endpoint.Name.Should().Be("test-e2e-three");
        }

        [Fact]
        public async Task E2E_OutboundRouting_StaticBroadcasting()
        {
            _configurator.Connect(endpoints => endpoints
                .AddOutbound<TestEventOne>(
                    new KafkaProducerEndpoint("test-e2e-one"),
                    new KafkaProducerEndpoint("test-e2e-two"),
                    new KafkaProducerEndpoint("test-e2e-three")));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventTwo());
            await publisher.PublishAsync(new TestEventThree());

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(3);
            _spyBehavior.OutboundEnvelopes[0].Endpoint.Name.Should().Be("test-e2e-one");
            _spyBehavior.OutboundEnvelopes[1].Endpoint.Name.Should().Be("test-e2e-two");
            _spyBehavior.OutboundEnvelopes[2].Endpoint.Name.Should().Be("test-e2e-three");
        }

        [Fact]
        public async Task E2E_OutboundRouting_DynamicCustomRouting()
        {
            _configurator.Connect(endpoints => endpoints
                .AddOutbound<TestPrioritizedCommand, TestPrioritizedOutboundRouter>());

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<ICommandPublisher>();

            await publisher.ExecuteAsync(new TestPrioritizedCommand
                { Priority = TestPrioritizedCommand.PriorityEnum.Low });
            await publisher.ExecuteAsync(new TestPrioritizedCommand
                { Priority = TestPrioritizedCommand.PriorityEnum.Low });
            await publisher.ExecuteAsync(new TestPrioritizedCommand
                { Priority = TestPrioritizedCommand.PriorityEnum.High });

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(6);
            _spyBehavior.OutboundEnvelopes.Count(envelope => envelope.Endpoint.Name == "test-e2e-all")
                .Should().Be(3);
            _spyBehavior.OutboundEnvelopes.Count(envelope => envelope.Endpoint.Name == "test-e2e-low")
                .Should().Be(2);
            _spyBehavior.OutboundEnvelopes.Count(envelope => envelope.Endpoint.Name == "test-e2e-normal")
                .Should().Be(0);
            _spyBehavior.OutboundEnvelopes.Count(envelope => envelope.Endpoint.Name == "test-e2e-high")
                .Should().Be(1);
        }
    }
}