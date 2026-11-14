// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class BatchProcessingTests
{
    [Fact]
    public async Task Batch_ShouldReplayBatchFromFirstMessage_WhenConsumerIsStoppedAndRestarted()
    {
        TestingCollection<List<string?>> receivedBatches = [];
        TaskCompletionSource<bool> secondMessageReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(3, TimeSpan.FromMinutes(5)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message.ContentEventOne);

                if (list.Count == 2)
                    secondMessageReceived.TrySetResult(true);
            }
        }

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" });
        await secondMessageReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));

        await consumer.StopAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        DefaultClientSession.GetPendingMessagesCount().ShouldBe(2);

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });
        await consumer.StartAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].ShouldBe(["1", "2"]);
        receivedBatches[1].ShouldBe(["1", "2", "3"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }
}
