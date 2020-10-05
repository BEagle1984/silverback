// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Broker.Kafka
{
    [Trait("Category", "E2E")]
    public class KafkaBrokerBehaviorsPipelineTests : E2ETestFixture
    {
        [Fact]
        public async Task DefaultSettings_KafkaKeyAlwaysSet()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                                .AddInbound(new KafkaConsumerEndpoint("test-e2e")))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes[0].Headers[KafkaMessageHeaders.KafkaMessageKey]
                .Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task DefaultSettings_MessageIdUsedAsKafkaKey()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                                .AddInbound(new KafkaConsumerEndpoint("test-e2e")))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);
            var inboundMessageId = SpyBehavior.InboundEnvelopes[0].Headers[DefaultMessageHeaders.MessageId];
            var inboundKafkaKey = SpyBehavior.InboundEnvelopes[0].Headers[KafkaMessageHeaders.KafkaMessageKey];
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

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                                .AddInbound(new KafkaConsumerEndpoint("test-e2e")))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes[0].Headers[KafkaMessageHeaders.KafkaMessageKey].Should().Be("my-key");
        }

        [Fact]
        public async Task Chunks_SameKafkaKeySetToAllChunks()
        {
            var message = new KafkaEventOne
            {
                KafkaKey = "my-key",
                Content = "Hello E2E!"
            };

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka()) // <- this adds all extra behaviors, even though the InMemoryBroker will be used
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(
                                    new KafkaProducerEndpoint("test-e2e")
                                    {
                                        Chunk = new ChunkSettings
                                        {
                                            Size = 10
                                        }
                                    })
                                .AddInbound(new KafkaConsumerEndpoint("test-e2e")))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes.ForEach(
                envelope =>
                    envelope.Headers[KafkaMessageHeaders.KafkaMessageKey].Should().Be("my-key"));
        }
    }
}
