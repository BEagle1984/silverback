// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ProducerEndpointTests
{
    [Fact]
    public async Task ProducerEndpoint_ShouldTagActivityWithKafkaKeyAndOffset()
    {
        ConcurrentQueue<Activity> stoppedActivities = [];
        using ActivityListener listener = new()
        {
            ShouldListenTo = source => source.Name == "Silverback.Integration.Produce",
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stoppedActivities.Enqueue
        };
        ActivitySource.AddActivityListener(listener);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer.Produce<TestEventOne>(endpoint => endpoint
                    .ProduceTo(DefaultTopicName)
                    .SetKafkaKey(message => message?.ContentEventOne)))));

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        ActivityTraceId traceId;

        using (Activity activity = new("TestActivity"))
        {
            activity.Start();
            traceId = activity.TraceId;

            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "one" });
        }

        Activity produceActivity = stoppedActivities.Where(activity => activity.TraceId == traceId).ShouldHaveSingleItem();
        produceActivity.GetTagItem("messaging.destination").ShouldBe(DefaultTopicName);
        produceActivity.GetTagItem("messaging.message_id").ShouldBe($"{DefaultTopicName}[0]@0");
        produceActivity.GetTagItem("messaging.kafka.partition").ShouldBe($"{DefaultTopicName}[0]");
        produceActivity.GetTagItem("messaging.kafka.message_key").ShouldBe("one");
    }
}
