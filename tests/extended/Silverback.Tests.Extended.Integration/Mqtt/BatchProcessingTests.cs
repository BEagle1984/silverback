// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MQTTnet.Protocol;
using Shouldly;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Tests.Extended.Integration.TestHost;
using Silverback.Tests.Extended.Integration.TestHost.Mqtt;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Extended.Integration.Mqtt;

[Collection(MqttCollection.Name)]
[Trait("Type", "Integration")]
[Trait("Dependency", "Docker")]
[Trait("Broker", "MQTT")]
public class BatchProcessingTests : BrokerHarnessTests<MqttTestHarness>
{
    // Longer than the harness timeout, so that a batch that isn't completed by its size makes the test fail
    private static readonly TimeSpan UnreachableMaxWaitTime = TimeSpan.FromMinutes(5);

    // Created before the host, so that it records the deliveries before Silverback handles them (Silverback can block on backpressure)
    private readonly MqttClientRecorder _recorder;

    private MqttQualityOfServiceLevel _qualityOfServiceLevel;

    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Declares the fixture starting the infrastructure, so that xUnit verifies that the collection provides it")]
    [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Declares the fixture starting the infrastructure, so that xUnit verifies that the collection provides it")]
    public BatchProcessingTests(MqttFixture fixture, ITestOutputHelper output)
        : base(new MqttTestHarness("batch", output))
    {
        _recorder = new MqttClientRecorder(Harness.NativeClient);
    }

    private enum BatchState
    {
        Pending,

        Enumerated,

        Completed,

        Aborted
    }

    private TestMqttClient Publisher
    {
        get => field ?? throw new InvalidOperationException("The consumer isn't started.");
        set;
    }

    [Theory]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce)]
    [InlineData(MqttQualityOfServiceLevel.ExactlyOnce)]
    public async Task Batch_ShouldAcknowledgeEveryDelivery_WhenBatchesAreProcessed(MqttQualityOfServiceLevel qualityOfServiceLevel)
    {
        BatchRecorder batches = new();
        await StartConsumerAsync(qualityOfServiceLevel, batches, 5, TimeSpan.FromSeconds(1));

        // The last batch is completed by the max wait time
        await PublishAsync(1, 23);
        await Harness.WaitUntilAsync(() => batches.GetBatches(BatchState.Completed).Length == 5, "the batches to be processed");
        await WaitUntilAcknowledgedAsync(23);

        batches.GetBatches(BatchState.Completed).ShouldBe(
        [
            [1, 2, 3, 4, 5],
            [6, 7, 8, 9, 10],
            [11, 12, 13, 14, 15],
            [16, 17, 18, 19, 20],
            [21, 22, 23]
        ]);
        batches.GetBatches(BatchState.Aborted).ShouldBeEmpty();
        _recorder.Disconnections.ShouldBe(0);

        await VerifyNoUnacknowledgedDeliveriesAsync();
        Harness.Logs.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce)]
    [InlineData(MqttQualityOfServiceLevel.ExactlyOnce)]
    public async Task Batch_ShouldAcknowledgeConcurrentBatchesOutOfOrder_WhenProcessingInParallel(
        MqttQualityOfServiceLevel qualityOfServiceLevel)
    {
        // The batches of the first round are held until released, by their first message
        Dictionary<int, TaskCompletionSource<bool>> gates = new()
        {
            [1] = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously),
            [2] = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously),
            [3] = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously)
        };

        // Releases the gates when the test ends, even if it fails, since a held batch would block the shutdown
        Harness.OnDispose(() =>
        {
            foreach (TaskCompletionSource<bool> gate in gates.Values)
            {
                gate.TrySetResult(true);
            }
        });
        BatchRecorder batches = new(numbers => gates.TryGetValue(numbers[0], out TaskCompletionSource<bool>? gate)
            ? gate.Task.WaitAsync(Harness.Timeout)
            : Task.CompletedTask);
        await StartConsumerAsync(
            qualityOfServiceLevel,
            batches,
            4,
            UnreachableMaxWaitTime,
            client => client.EnableParallelProcessing(3));

        await PublishAsync(1, 12);
        await Harness.WaitUntilAsync(() => batches.GetBatches(BatchState.Enumerated).Length == 3, "the concurrent batches to be filled");

        // The messages are spread round-robin across the parallel batches
        batches.GetBatches(BatchState.Enumerated).OrderBy(numbers => numbers[0]).ShouldBe(
        [
            [1, 4, 7, 10],
            [2, 5, 8, 11],
            [3, 6, 9, 12]
        ]);

        // Complete the batches in the reverse order, so that the later deliveries are acknowledged first
        foreach (int first in new[] { 3, 2, 1 })
        {
            gates[first].TrySetResult(true);
            int[] batch = [first, first + 3, first + 6, first + 9];
            await Harness.WaitUntilAsync(
                () => batch.All(GetAcknowledgedNumbers().Contains),
                $"the batch starting with {first} to be acknowledged");
        }

        GetAcknowledgedNumbers().ShouldBe([3, 6, 9, 12, 2, 5, 8, 11, 1, 4, 7, 10]);

        // The broker accepted the acknowledgments: the client is still connected and the consumption continues
        await PublishAsync(13, 24);
        await Harness.WaitUntilAsync(() => batches.GetBatches(BatchState.Completed).Length == 6, "the second round of batches");
        await WaitUntilAcknowledgedAsync(24);

        int[][] completedBatches = batches.GetBatches(BatchState.Completed);
        completedBatches.ShouldAllBe(numbers => numbers.Length == 4);
        completedBatches.SelectMany(numbers => numbers).Order().ShouldBe(Enumerable.Range(1, 24));
        _recorder.Disconnections.ShouldBe(0);
        _recorder.GetDeliveries().ShouldAllBe(delivery => !delivery.IsDuplicate);

        await VerifyNoUnacknowledgedDeliveriesAsync();
        Harness.Logs.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce, false)]
    [InlineData(MqttQualityOfServiceLevel.ExactlyOnce, false)]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce, true)]
    [InlineData(MqttQualityOfServiceLevel.ExactlyOnce, true)]
    public async Task Batch_ShouldProcessReplayedBatch_WhenSessionIsResumedWhileBatchIsPending(
        MqttQualityOfServiceLevel qualityOfServiceLevel,
        bool loseConnection)
    {
        BatchRecorder batches = new();
        DisconnectableTcpProxy? proxy = loseConnection ? Harness.UseDisconnectableProxy() : null;
        await StartConsumerAsync(qualityOfServiceLevel, batches, 5, UnreachableMaxWaitTime);

        await PublishAsync(1, 3);
        await Harness.WaitUntilAsync(() => batches.GetBatches(BatchState.Pending) is [[1, 2, 3]], "the pending batch");

        if (proxy != null)
        {
            await LoseConnectionAsync(proxy);
            await PublishAsync(4, 5);
        }
        else
        {
            await Harness.DisconnectConsumerAsync();

            // Queued by the broker after the unacknowledged deliveries
            await PublishAsync(4, 4);
            await Harness.ReconnectAsync();
            await PublishAsync(5, 5);
        }

        await Harness.WaitUntilAsync(() => batches.GetBatches(BatchState.Completed).Length == 1, "the replayed batch");
        await PublishAsync(6, 10);
        await Harness.WaitUntilAsync(() => batches.GetBatches(BatchState.Completed).Length == 2, "the next batch");
        await WaitUntilAcknowledgedAsync(10);

        batches.GetBatches(BatchState.Aborted).ShouldBe([[1, 2, 3]]);
        batches.GetBatches(BatchState.Completed).ShouldBe(
        [
            [1, 2, 3, 4, 5],
            [6, 7, 8, 9, 10]
        ]);
        _recorder.Connections.ShouldBe(2);
        _recorder.IsSessionPresent.ShouldBeTrue();

        // The broker replays the unacknowledged deliveries, in order, before the later messages
        GetDeliveredNumbers(2).ShouldBe(Enumerable.Range(1, 10));

        await VerifyNoUnacknowledgedDeliveriesAsync();
        Harness.Logs.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce)]
    [InlineData(MqttQualityOfServiceLevel.ExactlyOnce)]
    public async Task Batch_ShouldProcessReplayedDeliveries_WhenConnectionIsLostWhileAcknowledgingBatch(
        MqttQualityOfServiceLevel qualityOfServiceLevel)
    {
        BatchRecorder batches = new();
        DisconnectableTcpProxy proxy = Harness.UseDisconnectableProxy();
        await StartConsumerAsync(qualityOfServiceLevel, batches, 5, TimeSpan.FromSeconds(1));

        // The connection is cut right before the third acknowledgment of the batch is sent, while the previous ones may or may not have
        // reached the broker
        int subscriptionsCount = Harness.SubscriptionsCount;
        _recorder.InvokeBeforeAcknowledgment(3, proxy.DropConnections);
        await PublishAsync(1, 5);
        await Harness.WaitUntilSubscribedAsync(subscriptionsCount + 1);

        await PublishAsync(6, 10);
        await Harness.WaitUntilAsync(
            () => GetDeliveredNumbers(2).Contains(10) && IsEveryDeliveryAcknowledged(),
            "the replayed and the next deliveries to be acknowledged");

        // The broker replays, in order and before the later messages, the deliveries whose acknowledgment didn't reach it: at least the
        // ones whose acknowledgment wasn't sent
        int[] replayed = GetDeliveredNumbers(2);
        int firstReplayed = replayed[0];
        firstReplayed.ShouldBeLessThanOrEqualTo(3);
        replayed.ShouldBe(Enumerable.Range(firstReplayed, 11 - firstReplayed));
        _recorder.GetDeliveries()
            .Where(delivery => delivery.Connection == 2 && GetNumber(delivery.Payload) <= 5)
            .ShouldAllBe(delivery => delivery.IsDuplicate);

        // The batch was processed before the connection was lost, and the replayed deliveries are processed again
        int[][] completedBatches = batches.GetBatches(BatchState.Completed);
        completedBatches[0].ShouldBe([1, 2, 3, 4, 5]);
        completedBatches.SelectMany(numbers => numbers).Order().ShouldBe(
            Enumerable.Range(1, 10).Concat(Enumerable.Range(firstReplayed, 6 - firstReplayed)).Order());
        batches.GetBatches(BatchState.Aborted).ShouldBeEmpty();
        _recorder.Connections.ShouldBe(2);
        _recorder.IsSessionPresent.ShouldBeTrue();

        await VerifyNoUnacknowledgedDeliveriesAsync();
        Harness.Logs.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task Batch_ShouldKeepConsuming_WhenReceiveMaximumEqualsBatchSizeTimesParallelism()
    {
        const int batchSize = 4;
        const int maxDegreeOfParallelism = 3;
        const int receiveMaximum = batchSize * maxDegreeOfParallelism;
        const int messagesCount = 120;
        BatchRecorder batches = new();
        await StartConsumerAsync(
            MqttQualityOfServiceLevel.AtLeastOnce,
            batches,
            batchSize,
            UnreachableMaxWaitTime,
            client => client
                .EnableParallelProcessing(maxDegreeOfParallelism)
                .LimitUnacknowledgedPublications(receiveMaximum));

        // Queued while disconnected, so that the broker fills the receive maximum as soon as the session is resumed
        await Harness.DisconnectConsumerAsync();
        await PublishAsync(1, messagesCount);
        await Harness.ReconnectAsync();

        await Harness.WaitUntilAsync(
            () => batches.GetBatches(BatchState.Completed).Length == messagesCount / batchSize,
            "all the batches to be processed");
        await WaitUntilAcknowledgedAsync(messagesCount);

        // Each batch is completed by its size, never by the max wait time
        int[][] completedBatches = batches.GetBatches(BatchState.Completed);
        completedBatches.ShouldAllBe(numbers => numbers.Length == batchSize);
        completedBatches.ShouldAllBe(numbers => numbers.SequenceEqual(numbers.Order()));
        completedBatches.SelectMany(numbers => numbers).Order().ShouldBe(Enumerable.Range(1, messagesCount));
        _recorder.MaxInFlight.ShouldBe(receiveMaximum);

        await VerifyNoUnacknowledgedDeliveriesAsync();
        Harness.Logs.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task Batch_ShouldCompleteBatchByMaxWaitTime_WhenBrokerLimitsUnacknowledgedDeliveries()
    {
        // The configuration doesn't allow a client receive maximum lower than the batch size, but the broker enforces its own limit
        // (max_inflight) regardless of the receive maximum requested by the client
        int maxInflight = await MqttFixture.GetMaxInflightAsync();
        int batchSize = maxInflight * 2;
        int messagesCount = (maxInflight * 2) + (maxInflight / 2);
        BatchRecorder batches = new();
        await StartConsumerAsync(MqttQualityOfServiceLevel.AtLeastOnce, batches, batchSize, TimeSpan.FromSeconds(2));

        // Queued while disconnected, so that the broker fills its in-flight window as soon as the session is resumed
        await Harness.DisconnectConsumerAsync();
        await PublishAsync(1, messagesCount);
        await Harness.ReconnectAsync();

        await Harness.WaitUntilAsync(
            () => batches.GetBatches(BatchState.Completed).Sum(numbers => numbers.Length) == messagesCount,
            "all the messages to be processed");
        await WaitUntilAcknowledgedAsync(messagesCount);

        // Each batch gets what the broker lets through and is completed by the max wait time, which releases the next deliveries
        batches.GetBatches(BatchState.Completed).Select(numbers => numbers.Length).ShouldBe([maxInflight, maxInflight, maxInflight / 2]);
        batches.GetBatches(BatchState.Completed).SelectMany(numbers => numbers).ShouldBe(Enumerable.Range(1, messagesCount));
        _recorder.MaxInFlight.ShouldBe(maxInflight);

        await VerifyNoUnacknowledgedDeliveriesAsync();
        Harness.Logs.Errors.ShouldBeEmpty();
    }

    private static int GetNumber(byte[] payload) => JsonSerializer.Deserialize<BatchMessage>(payload)!.Number;

    // Starts a Silverback client consuming batches with a persistent session, plus a native client publishing the messages
    private async Task StartConsumerAsync(
        MqttQualityOfServiceLevel qualityOfServiceLevel,
        BatchRecorder batches,
        int batchSize,
        TimeSpan maxWaitTime,
        Action<MqttClientConfigurationBuilder>? configureClient = null)
    {
        _qualityOfServiceLevel = qualityOfServiceLevel;

        Harness.AddClient(client =>
        {
            client.RequestPersistentSession(TimeSpan.FromMinutes(5))
                .Consume<BatchMessage>(endpoint => endpoint
                    .ConsumeFrom(Harness.Prefix)
                    .WithQualityOfServiceLevel(qualityOfServiceLevel)
                    .EnableBatchProcessing(batchSize, maxWaitTime)
                    .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader()));

            configureClient?.Invoke(client);
        });
        Harness.ConfigureSilverback(silverback => silverback.AddDelegateSubscriber<IAsyncEnumerable<BatchMessage>>(batches.HandleAsync));

        // Written once the clients are stopped, so that the dump includes the batches aborted by the stop and the packets exchanged while
        // disconnecting
        Harness.OnStopped(() =>
        {
            Harness.Output.WriteLine($"Client={Harness.Prefix}; batches: {batches}");
            WriteRecorder();
            return Task.CompletedTask;
        });

        await Harness.InitializeAsync();
        await Harness.ConnectAsync();
        Publisher = await Harness.CreateClientAsync("publisher");
    }

    private async Task PublishAsync(int from, int to)
    {
        for (int number = from; number <= to; number++)
        {
            await Publisher.PublishJsonAsync(Harness.Prefix, new BatchMessage { Number = number }, _qualityOfServiceLevel);
        }
    }

    // Cuts the connection and waits until the client reconnects
    private Task LoseConnectionAsync(DisconnectableTcpProxy proxy)
    {
        int subscriptionsCount = Harness.SubscriptionsCount;
        proxy.DropConnections();

        return Harness.WaitUntilSubscribedAsync(subscriptionsCount + 1);
    }

    // The numbers of the messages delivered with the specified connection, in the order they were received
    private int[] GetDeliveredNumbers(int connection) =>
    [
        .. _recorder.GetDeliveries()
            .Where(delivery => delivery.Connection == connection)
            .Select(delivery => GetNumber(delivery.Payload))
    ];

    // The numbers of the messages acknowledged with the current connection, in the order they were acknowledged
    private int[] GetAcknowledgedNumbers()
    {
        int connection = _recorder.Connections;
        Dictionary<ushort, int> numbers = [];

        foreach (MqttClientRecorder.Delivery delivery in _recorder.GetDeliveries().Where(delivery => delivery.Connection == connection))
        {
            numbers[delivery.PacketIdentifier] = GetNumber(delivery.Payload);
        }

        return
        [
            .. _recorder.GetAcknowledgments()
                .Where(acknowledgment => acknowledgment.Connection == connection)
                .Select(acknowledgment => numbers.GetValueOrDefault(acknowledgment.PacketIdentifier))
        ];
    }

    // Waits until the specified number of deliveries was received with the current connection and each of them was acknowledged
    // exactly once (and completed, with QoS 2)
    private Task WaitUntilAcknowledgedAsync(int count) =>
        Harness.WaitUntilAsync(() => IsAcknowledged(count), $"{count} deliveries to be acknowledged");

    // Whether each delivery received with the current connection was acknowledged exactly once (and completed, with QoS 2). Unlike
    // WaitUntilAcknowledgedAsync, it ignores the additional completions: when the session is resumed, the broker sends again the
    // PUBREL of the QoS 2 deliveries whose PUBREC it received with the previous connection, but not their PUBCOMP.
    private bool IsEveryDeliveryAcknowledged()
    {
        int connection = _recorder.Connections;
        ushort[] delivered = [.. _recorder.GetDeliveries().Where(item => item.Connection == connection).Select(item => item.PacketIdentifier)];
        ushort[] acknowledged = [.. _recorder.GetAcknowledgments().Where(item => item.Connection == connection).Select(item => item.PacketIdentifier)];

        if (!delivered.Order().SequenceEqual(acknowledged.Order()))
            return false;

        if (_qualityOfServiceLevel != MqttQualityOfServiceLevel.ExactlyOnce)
            return true;

        HashSet<ushort> completed = [.. _recorder.GetCompletions().Where(item => item.Connection == connection).Select(item => item.PacketIdentifier)];

        return delivered.All(completed.Contains);
    }

    // Disconnects the client and resumes its session with a native client: the broker must not replay any delivery, nor release any
    // QoS 2 delivery whose completion it didn't receive
    private async Task VerifyNoUnacknowledgedDeliveriesAsync()
    {
        await Harness.DisconnectConsumerAsync();

        SessionReplay replay = await Harness.ResumeAndEndSessionAsync(Harness.Prefix);

        replay.IsSessionPresent.ShouldBeTrue();
        replay.Payloads.Select(GetNumber).ShouldBeEmpty();
        replay.Releases.ShouldBe(0);
    }

    private bool IsAcknowledged(int count)
    {
        int connection = _recorder.Connections;
        ushort[] delivered = [.. _recorder.GetDeliveries().Where(item => item.Connection == connection).Select(item => item.PacketIdentifier)];
        ushort[] acknowledged = [.. _recorder.GetAcknowledgments().Where(item => item.Connection == connection).Select(item => item.PacketIdentifier)];

        if (delivered.Length != count || acknowledged.Length != count || !delivered.Order().SequenceEqual(acknowledged.Order()))
            return false;

        if (_qualityOfServiceLevel != MqttQualityOfServiceLevel.ExactlyOnce)
            return true;

        ushort[] completed = [.. _recorder.GetCompletions().Where(item => item.Connection == connection).Select(item => item.PacketIdentifier)];

        return delivered.Order().SequenceEqual(completed.Order());
    }

    private void WriteRecorder()
    {
        Harness.Output.WriteLine(
            $"Connections={_recorder.Connections}; disconnections={_recorder.Disconnections}; " +
            $"session present={_recorder.IsSessionPresent}; max in flight={_recorder.MaxInFlight}");
        Harness.Output.WriteLine(
            "Deliveries: " +
            string.Join(", ", _recorder.GetDeliveries().Select(delivery =>
                $"{GetNumber(delivery.Payload)} (connection {delivery.Connection}, id {delivery.PacketIdentifier}" +
                $"{(delivery.IsDuplicate ? ", dup" : string.Empty)})")));
        Harness.Output.WriteLine(
            "Acknowledgments: " +
            string.Join(", ", _recorder.GetAcknowledgments().Select(item => $"{item.PacketIdentifier} (connection {item.Connection})")));
    }

    // Records the batches received by the subscriber, optionally holding each batch after it was enumerated
    private sealed class BatchRecorder
    {
        private readonly System.Threading.Lock _lock = new();

        private readonly List<RecordedBatch> _batches = [];

        private readonly Func<int[], Task>? _onEnumerated;

        public BatchRecorder(Func<int[], Task>? onEnumerated = null)
        {
            _onEnumerated = onEnumerated;
        }

        public async Task HandleAsync(IAsyncEnumerable<BatchMessage> batch)
        {
            RecordedBatch recordedBatch = new();

            lock (_lock)
            {
                _batches.Add(recordedBatch);
            }

            try
            {
                await foreach (BatchMessage message in batch)
                {
                    lock (_lock)
                    {
                        recordedBatch.Numbers.Add(message.Number);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // The pending batch is aborted when the client disconnects
                SetState(recordedBatch, BatchState.Aborted);
                throw;
            }

            SetState(recordedBatch, BatchState.Enumerated);

            if (_onEnumerated != null)
                await _onEnumerated(GetNumbers(recordedBatch));

            SetState(recordedBatch, BatchState.Completed);
        }

        // The numbers of the batches in the specified state, in the order the batches were started
        public int[][] GetBatches(BatchState state)
        {
            lock (_lock)
            {
                return [.. _batches.Where(batch => batch.State == state).Select(batch => batch.Numbers.ToArray())];
            }
        }

        public override string ToString()
        {
            lock (_lock)
            {
                return string.Join("; ", _batches.Select(batch => $"{batch.State} [{string.Join(", ", batch.Numbers)}]"));
            }
        }

        private int[] GetNumbers(RecordedBatch batch)
        {
            lock (_lock)
            {
                return [.. batch.Numbers];
            }
        }

        private void SetState(RecordedBatch batch, BatchState state)
        {
            lock (_lock)
            {
                batch.State = state;
            }
        }
    }

    private sealed class RecordedBatch
    {
        public List<int> Numbers { get; } = [];

        public BatchState State { get; set; }
    }

    private sealed class BatchMessage
    {
        public int Number { get; init; }
    }
}
