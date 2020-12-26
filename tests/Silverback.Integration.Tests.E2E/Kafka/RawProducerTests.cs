// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class RawProducerTests : KafkaTestFixture
    {
        public RawProducerTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

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
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            })))
                        .AddIntegrationSpy())
                .Run();

            var producer = Helper.Broker.GetProducer(
                new KafkaProducerEndpoint(
                    DefaultTopicName,
                    new KafkaClientConfig
                    {
                        BootstrapServers = "PLAINTEXT://e2e"
                    }));
            await producer.ProduceAsync(rawMessageStream, headers);

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
            Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().BeEquivalentTo("Hello E2E!");
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
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            })))
                        .AddIntegrationSpy())
                .Run();

            var producer = Helper.Broker.GetProducer(
                new KafkaProducerEndpoint(
                    DefaultTopicName,
                    new KafkaClientConfig
                    {
                        BootstrapServers = "PLAINTEXT://e2e"
                    }));
            await producer.ProduceAsync(rawMessage, headers);

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
            Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().BeEquivalentTo("Hello E2E!");
        }
    }
}
