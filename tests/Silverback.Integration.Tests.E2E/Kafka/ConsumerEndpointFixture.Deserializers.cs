// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ConsumerEndpointFixture
{
    [Fact]
    public async Task ConsumerEndpoint_ShouldConsumeStringMessages()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    kafkaClientsConfigurationBuilder => kafkaClientsConfigurationBuilder
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume<StringMessage<TestEventOne>>("topic1", endpoint => endpoint.ConsumeFrom("topic1"))
                                .Consume<StringMessage>("topic2", endpoint => endpoint.ConsumeFrom("topic2"))
                                .Consume(
                                    "topic3",
                                    endpoint => endpoint
                                        .ConsumeFrom("topic3")
                                        .ConsumeStrings(deserializer => deserializer.UseDiscriminator<TestEventThree>()))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer1 = Helper.GetProducerForEndpoint("topic1");
        IProducer producer2 = Helper.GetProducerForEndpoint("topic2");
        IProducer producer3 = Helper.GetProducerForEndpoint("topic3");

        await producer1.RawProduceAsync("Message for topic1"u8.ToArray());
        await producer2.RawProduceAsync("Message for topic2"u8.ToArray());
        await producer3.RawProduceAsync("Message for topic3"u8.ToArray());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        StringMessage[] messages = Helper.Spy.InboundEnvelopes.Select(envelope => envelope.Message).OfType<StringMessage>().ToArray();
        messages.Length.ShouldBe(3);
        messages.ShouldBe(
            [
                new StringMessage<TestEventOne>("Message for topic1"),
                new StringMessage("Message for topic2"),
                new StringMessage<TestEventThree>("Message for topic3")
            ],
            ignoreOrder: true);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldConsumeStringMessagesWithNullContent()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    kafkaClientsConfigurationBuilder => kafkaClientsConfigurationBuilder
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume<StringMessage<TestEventOne>>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.RawProduceAsync((byte[]?)null);
        await producer.RawProduceAsync((byte[]?)null);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        StringMessage[] messages = Helper.Spy.InboundEnvelopes.Select(envelope => envelope.Message).OfType<StringMessage>().ToArray();
        messages.Length.ShouldBe(2);
        messages.ShouldBe(
            [
                new StringMessage<TestEventOne>(null),
                new StringMessage<TestEventOne>(null)
            ],
            ignoreOrder: true);
    }
}
