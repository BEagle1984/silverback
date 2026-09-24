// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Buffers;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Testing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E;

[Trait("Type", "E2E")]
[Trait("Broker", "Kafka")]
[Trait("Broker", "MQTT")]
public class BrokerClientsShutdownTests(ITestOutputHelper testOutputHelper) : E2ETests(testOutputHelper)
{
    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(false, false, false, true)]
    [InlineData(false, true, false, false)]
    [InlineData(false, true, false, true)]
    [InlineData(true, false, false, false)]
    [InlineData(true, false, false, true)]
    [InlineData(true, true, false, false)]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]
    [InlineData(true, true, true, true)]
    public async Task StopAsync_ShouldAllowInFlightSubscriberToPublishBeforeDisconnectingClients(
        bool consumeMqtt,
        bool produceMqtt,
        bool shareMqttClient,
        bool batchProcessing)
    {
        const string inputTopic = "shutdown-input";
        const string outputTopic = "shutdown-output";
        TimeSpan timeout = TimeSpan.FromSeconds(15);
        TaskCompletionSource<bool> started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> stopping = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> published = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await Host.ConfigureServicesAndRunAsync(services =>
        {
            SilverbackBuilder silverback = services.AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options
                    .AddMockedKafka(kafka => kafka.WithDefaultPartitionsCount(1))
                    .AddMockedMqtt());

            silverback.AddKafkaClients(clients =>
            {
                clients.WithBootstrapServers("PLAINTEXT://e2e");

                if (!consumeMqtt)
                {
                    clients.AddConsumer(consumer => consumer
                        .WithGroupId("shutdown-group")
                        .Consume<TestEventOne>(endpoint =>
                        {
                            endpoint.ConsumeFrom(inputTopic);

                            if (batchProcessing)
                                endpoint.EnableBatchProcessing(1);
                        }));
                }

                if (!produceMqtt)
                {
                    clients.AddProducer(producer => producer
                        .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo(outputTopic)));
                }
            });

            silverback.AddMqttClients(clients =>
            {
                clients.ConnectViaTcp("e2e-mqtt-broker");

                if (consumeMqtt)
                {
                    clients.AddClient(client =>
                    {
                        client.WithClientId("shutdown-consumer")
                            .Consume<TestEventOne>(endpoint =>
                            {
                                endpoint.ConsumeFrom(inputTopic);

                                if (batchProcessing)
                                    endpoint.EnableBatchProcessing(1);
                            });

                        if (shareMqttClient)
                            client.Produce<TestEventTwo>(endpoint => endpoint.ProduceTo(outputTopic));
                    });
                }

                if (produceMqtt && !shareMqttClient)
                {
                    clients.AddClient(client => client
                        .WithClientId("shutdown-producer")
                        .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo(outputTopic)));
                }
            });

            if (batchProcessing)
            {
                silverback.AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>, IPublisher, CancellationToken>(async Task (batch, publisher, cancellationToken) =>
                {
                    TestEventOne? receivedMessage = null;

                    await foreach (TestEventOne message in batch)
                    {
                        receivedMessage = message;
                    }

                    await HandleEventAsync(receivedMessage!, publisher, cancellationToken);
                });
            }
            else
            {
                silverback.AddDelegateSubscriber<TestEventOne, IPublisher, CancellationToken>(HandleEventAsync);
            }
        });

        async Task HandleEventAsync(TestEventOne message, IPublisher publisher, CancellationToken cancellationToken)
        {
            using CancellationTokenRegistration registration = cancellationToken.Register(() => stopping.TrySetResult(true));
            started.TrySetResult(true);
            await release.Task.WaitAsync(timeout, CancellationToken.None);

            await publisher.PublishAsync(new TestEventTwo { ContentEventTwo = message.ContentEventOne }, cancellationToken: CancellationToken.None);
            published.TrySetResult(true);
        }

        IKafkaTestingHelper kafka = Host.ServiceProvider.GetRequiredService<IKafkaTestingHelper>();
        IMqttTestingHelper mqtt = Host.ServiceProvider.GetRequiredService<IMqttTestingHelper>();
        ITestingHelper inputHelper = consumeMqtt ? mqtt : kafka;
        IHost application = Host.ServiceProvider.GetRequiredService<IHost>();
        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        Producer producer = (Producer)Host.ServiceProvider.GetRequiredService<IProducerCollection>().Single();
        Task? shutdown = null;

        try
        {
            ReferenceEquals(consumer.Client, producer.Client).ShouldBe(shareMqttClient);

            await inputHelper.GetProducerForEndpoint(inputTopic)
                .ProduceAsync(new TestEventOne { ContentEventOne = "published-during-shutdown" });
            await started.Task.WaitAsync(timeout);

            shutdown = application.StopAsync();
            await stopping.Task.WaitAsync(timeout);

            shutdown.IsCompleted.ShouldBeFalse();
            consumer.Client.Status.ShouldBe(ClientStatus.Initialized);
            producer.Client.Status.ShouldBe(ClientStatus.Initialized);

            release.TrySetResult(true);
            await published.Task.WaitAsync(timeout);
            await shutdown.WaitAsync(timeout);

            byte[] output = produceMqtt
                ? mqtt.GetMessages(outputTopic).ShouldHaveSingleItem().Payload.ToArray()
                : kafka.GetTopic(outputTopic).GetAllMessages().ShouldHaveSingleItem().Value!;
            JsonSerializer.Deserialize<TestEventTwo>(output)!.ContentEventTwo.ShouldBe("published-during-shutdown");

            consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
            producer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        }
        finally
        {
            release.TrySetResult(true);

            if (shutdown != null)
                await shutdown.WaitAsync(timeout);
        }
    }
}
