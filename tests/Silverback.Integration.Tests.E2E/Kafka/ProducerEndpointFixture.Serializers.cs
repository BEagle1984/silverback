// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ProducerEndpointFixture
{
    [Fact]
    public async Task ProducerEndpoint_ShouldProduceStringMessages()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(producer => producer.Produce<StringMessage<TestEventOne>>(endpoint => endpoint.ProduceTo("topic1")))
                        .AddProducer(producer => producer.Produce<StringMessage<TestEventTwo>>(endpoint => endpoint.ProduceTo("topic2")))));

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync((StringMessage<TestEventOne>)"Message to topic1");
        await publisher.PublishAsync((StringMessage<TestEventTwo>)"Message to topic2");

        IInMemoryTopic topic1 = Helper.GetTopic("topic1");
        topic1.MessagesCount.ShouldBe(1);
        topic1.GetAllMessages()[0].Value.ShouldBe("Message to topic1"u8.ToArray());
        IInMemoryTopic topic2 = Helper.GetTopic("topic2");
        topic2.MessagesCount.ShouldBe(1);
        topic2.GetAllMessages()[0].Value.ShouldBe("Message to topic2"u8.ToArray());
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldProduceStringMessagesWithNullContent()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(producer => producer.Produce<StringMessage<TestEventOne>>(endpoint => endpoint.ProduceTo(DefaultTopicName)))));

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(StringMessage<TestEventOne>.FromString(null));
        await publisher.PublishAsync(StringMessage<TestEventOne>.FromString(null));

        IInMemoryTopic topic = Helper.GetTopic(DefaultTopicName);
        topic.MessagesCount.ShouldBe(2);
        topic.GetAllMessages()[0].Value.ShouldBe(null);
        topic.GetAllMessages()[1].Value.ShouldBe(null);
    }
}
