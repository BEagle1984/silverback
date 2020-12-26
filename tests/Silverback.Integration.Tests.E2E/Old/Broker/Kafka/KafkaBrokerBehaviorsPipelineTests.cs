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
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Old.Broker.Kafka
{
    public class KafkaBrokerBehaviorsPipelineTests : KafkaTestFixture
    {
        public KafkaBrokerBehaviorsPipelineTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact(Skip = "Deprecated")]
        public async Task DefaultSettings_KafkaKeyAlwaysSet()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                                .AddInbound(new KafkaConsumerEndpoint("test-e2e")))
                        .AddIntegrationSpy())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.InboundEnvelopes[0].Headers[KafkaMessageHeaders.KafkaMessageKey]
                .Should().NotBeNullOrEmpty();
        }

        [Fact(Skip = "Deprecated")]
        public async Task DefaultSettings_MessageIdUsedAsKafkaKey()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                                .AddInbound(new KafkaConsumerEndpoint("test-e2e")))
                        .AddIntegrationSpy())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            var inboundMessageId = Helper.Spy.InboundEnvelopes[0].Headers[DefaultMessageHeaders.MessageId];
            var inboundKafkaKey = Helper.Spy.InboundEnvelopes[0].Headers[KafkaMessageHeaders.KafkaMessageKey];
            inboundKafkaKey.Should().Be(inboundMessageId);
        }

        [Fact(Skip = "Deprecated")]
        public async Task DefaultSettings_ExplicitlySetKafkaKeyTransferred()
        {
            var message = new KafkaEventOne
            {
                KafkaKey = "my-key",
                Content = "Hello E2E!"
            };

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                                .AddInbound(new KafkaConsumerEndpoint("test-e2e")))
                        .AddIntegrationSpy())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.InboundEnvelopes[0].Headers[KafkaMessageHeaders.KafkaMessageKey].Should().Be("my-key");
        }

        [Fact(Skip = "Deprecated")]
        public async Task Chunks_SameKafkaKeySetToAllChunks()
        {
            var message = new KafkaEventOne
            {
                KafkaKey = "my-key",
                Content = "Hello E2E!"
            };

            Host.ConfigureServices(
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
                        .AddIntegrationSpy())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.InboundEnvelopes.ForEach(
                envelope =>
                    envelope.Headers[KafkaMessageHeaders.KafkaMessageKey].Should().Be("my-key"));
        }
    }
}
