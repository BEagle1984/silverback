// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    [Trait("Category", "E2E-Kafka"), Trait("CI", "false")]
    public class KafkaBrokerBehaviorsPipelineTests
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly BusConfigurator _configurator;
        private readonly SpyBrokerBehavior _spyBehavior;

        public KafkaBrokerBehaviorsPipelineTests()
        {
            var services = new ServiceCollection();

            services
                .AddNullLogger()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options
                    .AddKafka()
                    .AddInMemoryChunkStore())
                .AddSingletonBrokerBehavior<SpyBrokerBehavior>();

            _serviceProvider = services.BuildServiceProvider();

            _configurator = _serviceProvider.GetRequiredService<BusConfigurator>();
            _spyBehavior = _serviceProvider.GetServices<IBrokerBehavior>().OfType<SpyBrokerBehavior>().First();
        }

        [Fact]
        public async Task DefaultSettings_KafkaKeyTransferred()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var rawMessage = await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            _configurator.Connect(endpoints => endpoints
                .AddOutbound<IIntegrationEvent>(
                    new KafkaProducerEndpoint("test-e2e"))
                .AddInbound(
                    new KafkaConsumerEndpoint("test-e2e")));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message);

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            var outboundKafkaKey = _spyBehavior.OutboundEnvelopes.First().Headers[KafkaMessageHeaders.KafkaMessageKey];
            outboundKafkaKey.Should().NotBeNullOrEmpty();
            _spyBehavior.InboundEnvelopes.Count.Should().Be(1);
            var inboundKafkaKey = _spyBehavior.InboundEnvelopes.First().Headers[KafkaMessageHeaders.KafkaMessageKey];
            inboundKafkaKey.Should().Be(outboundKafkaKey);
        }

        [Fact]
        public async Task DefaultSettings_MessageIdUsedAsKafkaKey()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var rawMessage = await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            _configurator.Connect(endpoints => endpoints
                .AddOutbound<IIntegrationEvent>(
                    new KafkaProducerEndpoint("test-e2e"))
                .AddInbound(
                    new KafkaConsumerEndpoint("test-e2e")));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message);

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            var outboundMessageId = _spyBehavior.OutboundEnvelopes.First().Headers[DefaultMessageHeaders.MessageId];
            var outboundKafkaKey = _spyBehavior.OutboundEnvelopes.First().Headers[KafkaMessageHeaders.KafkaMessageKey];
            outboundMessageId.Should().NotBeNullOrEmpty();
            outboundKafkaKey.Should().Be(outboundMessageId);
        }
        
        [Fact]
        public async Task DefaultSettings_ExplicitlySetKafkaKeyTransferred()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var rawMessage = await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            _configurator.Connect(endpoints => endpoints
                .AddOutbound<IIntegrationEvent>(
                    new KafkaProducerEndpoint("test-e2e"))
                .AddInbound(
                    new KafkaConsumerEndpoint("test-e2e")));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message);

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            var outboundMessageId = _spyBehavior.OutboundEnvelopes.First().Headers[DefaultMessageHeaders.MessageId];
            var outboundKafkaKey = _spyBehavior.OutboundEnvelopes.First().Headers[KafkaMessageHeaders.KafkaMessageKey];
            outboundMessageId.Should().NotBeNullOrEmpty();
            outboundKafkaKey.Should().Be(outboundMessageId);
        }


        [Fact]
        public async Task Chunks_SameKafkaKeySetToAllChunks()
        {
            
        }
        
    }
}