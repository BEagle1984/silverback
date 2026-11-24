// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.KafkaOffsetStore;

namespace Silverback.Tests.Integration.Kafka.Messaging.Consuming.KafkaOffsetStore;

// A real KafkaConsumer whose native client is a substitute. Once started, it reads the specified assignment, so that the offsets of the
// assigned partitions are owned (storable), while the consume loop polls nothing.
internal sealed class AssignedKafkaConsumer : IAsyncDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    // Never set: the native poll waits until its timeout and returns nothing
    private readonly ManualResetEventSlim _pollWaitHandle = new();

    private bool _isStarted;

    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public AssignedKafkaConsumer(KafkaConsumerConfiguration configuration, params TopicPartition[] assignment)
    {
        IConfluentConsumerWrapper client = Substitute.For<IConfluentConsumerWrapper>();
        client.Initialized.Returns(new AsyncEvent<BrokerClient>());
        client.Disconnecting.Returns(new AsyncEvent<BrokerClient>());
        client.Status.Returns(ClientStatus.Initialized);
        client.Assignment.Returns(assignment);
        client.Consume(Arg.Any<TimeSpan>()).Returns(call =>
        {
            _pollWaitHandle.Wait(call.Arg<TimeSpan>());

            return null;
        });

        IKafkaOffsetStore offsetStore = Substitute.For<IKafkaOffsetStore>();
        offsetStore.GetStoredOffsets(Arg.Any<string>()).Returns([]);
        IKafkaOffsetStoreFactory offsetStoreFactory = Substitute.For<IKafkaOffsetStoreFactory>();
        offsetStoreFactory.GetStore(Arg.Any<KafkaOffsetStoreSettings>(), Arg.Any<IServiceProvider>()).Returns(offsetStore);

        Consumer = new KafkaConsumer(
            "consumer1",
            client,
            configuration with { PollingTimeout = TimeSpan.FromMilliseconds(10) },
            Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>(),
            Substitute.For<IBrokerClientCallbacksInvoker>(),
            offsetStoreFactory,
            Substitute.For<IServiceProvider>(),
            Substitute.For<ISilverbackLogger<KafkaConsumer>>());
    }

    public KafkaConsumer Consumer { get; }

    public async Task StartAsync()
    {
        await Consumer.StartAsync().AsTask().WaitAsync(Timeout);
        _isStarted = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_isStarted)
            await Consumer.StopAsync().AsTask().WaitAsync(Timeout);

        await Task.Run(Consumer.Dispose).WaitAsync(Timeout);
        _pollWaitHandle.Dispose();
    }
}
