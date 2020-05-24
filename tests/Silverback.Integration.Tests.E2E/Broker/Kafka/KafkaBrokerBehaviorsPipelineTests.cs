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
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Broker.Kafka
{
    [Trait("Category", "E2E")]
    public class KafkaBrokerBehaviorsPipelineTests
    {
        private readonly ServiceProvider _serviceProvider;

        private readonly IBusConfigurator _configurator;

        private readonly SpyBrokerBehavior _spyBehavior;

        public KafkaBrokerBehaviorsPipelineTests()
        {
            var services = new ServiceCollection();

            services
                .AddNullLogger()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddKafka() // <- this adds all extra behaviors, even though the InMemoryBroker will be used
                        .AddInMemoryChunkStore())
                .AddSingletonBrokerBehavior<SpyBrokerBehavior>();

            services.OverrideWithInMemoryBroker();

            _serviceProvider = services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateScopes = true
                });

            _configurator = _serviceProvider.GetRequiredService<IBusConfigurator>();
            _spyBehavior = _serviceProvider.GetServices<IBrokerBehavior>().OfType<SpyBrokerBehavior>().First();
        }

        [Fact]
        public async Task DefaultSettings_KafkaKeyAlwaysSet()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };

            _configurator.Connect(
                endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                    .AddInbound(new KafkaConsumerEndpoint("test-e2e")));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message);

            _spyBehavior.InboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.InboundEnvelopes[0].Headers[KafkaMessageHeaders.KafkaMessageKey]
                .Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task DefaultSettings_MessageIdUsedAsKafkaKey()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };

            _configurator.Connect(
                endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                    .AddInbound(new KafkaConsumerEndpoint("test-e2e")));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message);

            _spyBehavior.InboundEnvelopes.Count.Should().Be(1);
            var inboundMessageId = _spyBehavior.InboundEnvelopes[0].Headers[DefaultMessageHeaders.MessageId];
            var inboundKafkaKey = _spyBehavior.InboundEnvelopes[0].Headers[KafkaMessageHeaders.KafkaMessageKey];
            inboundKafkaKey.Should().Be(inboundMessageId);
        }

        [Fact]
        public async Task DefaultSettings_ExplicitlySetKafkaKeyTransferred()
        {
            var message = new KafkaEventOne
            {
                KafkaKey = "my-key",
                Content = "Hello E2E!"
            };

            _configurator.Connect(
                endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                    .AddInbound(new KafkaConsumerEndpoint("test-e2e")));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message);

            _spyBehavior.InboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.InboundEnvelopes[0].Headers[KafkaMessageHeaders.KafkaMessageKey].Should().Be("my-key");
        }

        [Fact]
        public async Task Chunks_SameKafkaKeySetToAllChunks()
        {
            var message = new KafkaEventOne
            {
                KafkaKey = "my-key",
                Content = "Hello E2E!"
            };

            _configurator.Connect(
                endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(
                        new KafkaProducerEndpoint("test-e2e")
                        {
                            Chunk = new ChunkSettings
                            {
                                Size = 10
                            }
                        })
                    .AddInbound(new KafkaConsumerEndpoint("test-e2e")));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message);

            _spyBehavior.InboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.InboundEnvelopes.ForEach(
                envelope =>
                    envelope.Headers[KafkaMessageHeaders.KafkaMessageKey].Should().Be("my-key"));
        }
    }
}
