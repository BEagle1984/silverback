// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    [Trait("Category", "E2E")]
    public class RawProducerTests : E2ETestFixture
    {
        [Fact]
        public async Task RawProducer_Stream_ProducedAndConsumed()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var headers = new MessageHeaderCollection();
            Stream rawMessageStream = await Endpoint.DefaultSerializer.SerializeAsync(
                                          message,
                                          headers,
                                          MessageSerializationContext.Empty) ??
                                      throw new InvalidOperationException("Serializer returned null");

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        }
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var producer = Broker.GetProducer(new KafkaProducerEndpoint(DefaultTopicName));
            await producer.ProduceAsync(rawMessageStream, headers);

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);

            SpyBehavior.InboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
            SpyBehavior.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().BeEquivalentTo("Hello E2E!");
        }

        [Fact]
        public async Task RawProducer_ByteArray_ProducedAndConsumed()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var headers = new MessageHeaderCollection();
            byte[] rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                                    message,
                                    headers,
                                    MessageSerializationContext.Empty)).ReadAll() ??
                                throw new InvalidOperationException("Serializer returned null");

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        }
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var producer = Broker.GetProducer(new KafkaProducerEndpoint(DefaultTopicName));
            await producer.ProduceAsync(rawMessage, headers);

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);

            SpyBehavior.InboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
            SpyBehavior.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().BeEquivalentTo("Hello E2E!");
        }
    }
}
