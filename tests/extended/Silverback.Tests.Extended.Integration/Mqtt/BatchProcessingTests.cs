// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Tests.Extended.Integration.TestHost.Mqtt;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Extended.Integration.Mqtt;

[Collection(MqttCollection.Name)]
[Trait("Type", "Integration")]
[Trait("Dependency", "Docker")]
[Trait("Broker", "MQTT")]
public class BatchProcessingTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    // Longer than the test timeout, so that a batch that isn't completed by its size makes the test fail
    private static readonly TimeSpan UnreachableMaxWaitTime = TimeSpan.FromMinutes(5);

    private readonly ITestOutputHelper _output;

    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Declares the fixture starting the infrastructure, so that xUnit verifies that the collection provides it")]
    [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Declares the fixture starting the infrastructure, so that xUnit verifies that the collection provides it")]
    public BatchProcessingTests(MqttFixture fixture, ITestOutputHelper output)
    {
        _output = output;
    }

    private enum BatchState
    {
        Pending,

        Enumerated,

        Completed,

        Aborted
    }

    [Theory]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce)]
    [InlineData(MqttQualityOfServiceLevel.ExactlyOnce)]
    public async Task Batch_ShouldAcknowledgeEveryDelivery_WhenBatchesAreProcessed(MqttQualityOfServiceLevel qualityOfServiceLevel)
    {
        BatchRecorder batches = new();
        await using BatchConsumer consumer = new(_output, qualityOfServiceLevel, batches, 5, TimeSpan.FromSeconds(1));
        await consumer.StartAsync();

        // The last batch is completed by the max wait time
        await consumer.PublishAsync(1, 23);
        await WaitUntilAsync(() => batches.GetBatches(BatchState.Completed).Length == 5, "the batches to be processed");
        await consumer.WaitUntilAcknowledgedAsync(23);

        batches.GetBatches(BatchState.Completed).ShouldBe(
        [
            [1, 2, 3, 4, 5],
            [6, 7, 8, 9, 10],
            [11, 12, 13, 14, 15],
            [16, 17, 18, 19, 20],
            [21, 22, 23]
        ]);
        batches.GetBatches(BatchState.Aborted).ShouldBeEmpty();
        consumer.Recorder.Disconnections.ShouldBe(0);

        await consumer.VerifyNoUnacknowledgedDeliveriesAsync();
        consumer.Logs.Errors.ShouldBeEmpty();
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
        BatchRecorder batches = new(numbers => gates.TryGetValue(numbers[0], out TaskCompletionSource<bool>? gate)
            ? gate.Task.WaitAsync(Timeout)
            : Task.CompletedTask);
        await using BatchConsumer consumer = new(
            _output,
            qualityOfServiceLevel,
            batches,
            4,
            UnreachableMaxWaitTime,
            client => client.EnableParallelProcessing(3));

        try
        {
            await consumer.StartAsync();

            await consumer.PublishAsync(1, 12);
            await WaitUntilAsync(() => batches.GetBatches(BatchState.Enumerated).Length == 3, "the concurrent batches to be filled");

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
                await WaitUntilAsync(
                    () => batch.All(consumer.GetAcknowledgedNumbers().Contains),
                    $"the batch starting with {first} to be acknowledged");
            }

            consumer.GetAcknowledgedNumbers().ShouldBe([3, 6, 9, 12, 2, 5, 8, 11, 1, 4, 7, 10]);

            // The broker accepted the acknowledgments: the client is still connected and the consumption continues
            await consumer.PublishAsync(13, 24);
            await WaitUntilAsync(() => batches.GetBatches(BatchState.Completed).Length == 6, "the second round of batches");
            await consumer.WaitUntilAcknowledgedAsync(24);

            int[][] completedBatches = batches.GetBatches(BatchState.Completed);
            completedBatches.ShouldAllBe(numbers => numbers.Length == 4);
            completedBatches.SelectMany(numbers => numbers).Order().ShouldBe(Enumerable.Range(1, 24));
            consumer.Recorder.Disconnections.ShouldBe(0);
            consumer.Recorder.GetDeliveries().ShouldAllBe(delivery => !delivery.IsDuplicate);

            await consumer.VerifyNoUnacknowledgedDeliveriesAsync();
            consumer.Logs.Errors.ShouldBeEmpty();
        }
        finally
        {
            foreach (TaskCompletionSource<bool> gate in gates.Values)
            {
                gate.TrySetResult(true);
            }
        }
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
        await using DisconnectableTcpProxy? proxy = loseConnection ? new DisconnectableTcpProxy() : null;
        await using BatchConsumer consumer = new(
            _output,
            qualityOfServiceLevel,
            batches,
            5,
            UnreachableMaxWaitTime,
            proxyPort: proxy?.Port);
        await consumer.StartAsync();

        await consumer.PublishAsync(1, 3);
        await WaitUntilAsync(() => batches.GetBatches(BatchState.Pending) is [[1, 2, 3]], "the pending batch");

        if (proxy != null)
        {
            await consumer.LoseConnectionAsync(proxy);
            await consumer.PublishAsync(4, 5);
        }
        else
        {
            await consumer.DisconnectAsync();

            // Queued by the broker after the unacknowledged deliveries
            await consumer.PublishAsync(4, 4);
            await consumer.ReconnectAsync();
            await consumer.PublishAsync(5, 5);
        }

        await WaitUntilAsync(() => batches.GetBatches(BatchState.Completed).Length == 1, "the replayed batch");
        await consumer.PublishAsync(6, 10);
        await WaitUntilAsync(() => batches.GetBatches(BatchState.Completed).Length == 2, "the next batch");
        await consumer.WaitUntilAcknowledgedAsync(10);

        batches.GetBatches(BatchState.Aborted).ShouldBe([[1, 2, 3]]);
        batches.GetBatches(BatchState.Completed).ShouldBe(
        [
            [1, 2, 3, 4, 5],
            [6, 7, 8, 9, 10]
        ]);
        consumer.Recorder.Connections.ShouldBe(2);
        consumer.Recorder.IsSessionPresent.ShouldBeTrue();

        // The broker replays the unacknowledged deliveries, in order, before the later messages
        consumer.GetDeliveredNumbers(2).ShouldBe(Enumerable.Range(1, 10));

        await consumer.VerifyNoUnacknowledgedDeliveriesAsync();
        consumer.Logs.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task Batch_ShouldKeepConsuming_WhenReceiveMaximumEqualsBatchSizeTimesParallelism()
    {
        const int batchSize = 4;
        const int maxDegreeOfParallelism = 3;
        const int receiveMaximum = batchSize * maxDegreeOfParallelism;
        const int messagesCount = 120;
        BatchRecorder batches = new();
        await using BatchConsumer consumer = new(
            _output,
            MqttQualityOfServiceLevel.AtLeastOnce,
            batches,
            batchSize,
            UnreachableMaxWaitTime,
            client => client
                .EnableParallelProcessing(maxDegreeOfParallelism)
                .LimitUnacknowledgedPublications(receiveMaximum));
        await consumer.StartAsync();

        // Queued while disconnected, so that the broker fills the receive maximum as soon as the session is resumed
        await consumer.DisconnectAsync();
        await consumer.PublishAsync(1, messagesCount);
        await consumer.ReconnectAsync();

        await WaitUntilAsync(
            () => batches.GetBatches(BatchState.Completed).Length == messagesCount / batchSize,
            "all the batches to be processed");
        await consumer.WaitUntilAcknowledgedAsync(messagesCount);

        // Each batch is completed by its size, never by the max wait time
        int[][] completedBatches = batches.GetBatches(BatchState.Completed);
        completedBatches.ShouldAllBe(numbers => numbers.Length == batchSize);
        completedBatches.ShouldAllBe(numbers => numbers.SequenceEqual(numbers.Order()));
        completedBatches.SelectMany(numbers => numbers).Order().ShouldBe(Enumerable.Range(1, messagesCount));
        consumer.Recorder.MaxInFlight.ShouldBe(receiveMaximum);

        await consumer.VerifyNoUnacknowledgedDeliveriesAsync();
        consumer.Logs.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task Batch_ShouldCompleteBatchByMaxWaitTime_WhenBrokerLimitsUnacknowledgedDeliveries()
    {
        // The configuration doesn't allow a client receive maximum lower than the batch size, but the broker enforces its own limit
        // (max_inflight) regardless of the receive maximum requested by the client
        int maxInflight = await EmqxCluster.GetMaxInflightAsync();
        int batchSize = maxInflight * 2;
        int messagesCount = (maxInflight * 2) + (maxInflight / 2);
        BatchRecorder batches = new();
        await using BatchConsumer consumer = new(
            _output,
            MqttQualityOfServiceLevel.AtLeastOnce,
            batches,
            batchSize,
            TimeSpan.FromSeconds(2));
        await consumer.StartAsync();

        // Queued while disconnected, so that the broker fills its in-flight window as soon as the session is resumed
        await consumer.DisconnectAsync();
        await consumer.PublishAsync(1, messagesCount);
        await consumer.ReconnectAsync();

        await WaitUntilAsync(
            () => batches.GetBatches(BatchState.Completed).Sum(numbers => numbers.Length) == messagesCount,
            "all the messages to be processed");
        await consumer.WaitUntilAcknowledgedAsync(messagesCount);

        // Each batch gets what the broker lets through and is completed by the max wait time, which releases the next deliveries
        batches.GetBatches(BatchState.Completed).Select(numbers => numbers.Length).ShouldBe([maxInflight, maxInflight, maxInflight / 2]);
        batches.GetBatches(BatchState.Completed).SelectMany(numbers => numbers).ShouldBe(Enumerable.Range(1, messagesCount));
        consumer.Recorder.MaxInFlight.ShouldBe(maxInflight);

        await consumer.VerifyNoUnacknowledgedDeliveriesAsync();
        consumer.Logs.Errors.ShouldBeEmpty();
    }

    private static async Task WaitUntilAsync(Func<bool> condition, string description)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (!condition())
        {
            stopwatch.Elapsed.ShouldBeLessThan(Timeout, $"Timed out waiting for {description}");
            await Task.Delay(50);
        }
    }

    private static int GetNumber(byte[] payload) => JsonSerializer.Deserialize<BatchMessage>(payload)!.Number;

    // A Silverback host consuming batches with a persistent session, plus a native client publishing the messages
    private sealed class BatchConsumer : IAsyncDisposable
    {
        private readonly ITestOutputHelper _output;

        private readonly MqttQualityOfServiceLevel _qualityOfServiceLevel;

        private readonly BatchRecorder _batches;

        private readonly RecordingMqttClientFactory _clientFactory = new();

        private readonly IMqttClient _publisher = new MqttClientFactory().CreateMqttClient();

        private readonly IHost _host;

        private TaskCompletionSource<bool> _subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private MqttConsumer? _consumer;

        public BatchConsumer(
            ITestOutputHelper output,
            MqttQualityOfServiceLevel qualityOfServiceLevel,
            BatchRecorder batches,
            int batchSize,
            TimeSpan maxWaitTime,
            Action<MqttClientConfigurationBuilder>? configureClient = null,
            int? proxyPort = null)
        {
            _output = output;
            _qualityOfServiceLevel = qualityOfServiceLevel;
            _batches = batches;

            HostApplicationBuilder builder = Host.CreateApplicationBuilder();
            builder.Logging.ClearProviders().AddProvider(Logs);
            builder.Services.AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMqtt())
                .AddMqttClients(clients => clients
                    .ConnectViaTcp(proxyPort == null ? MqttFixture.BrokerHost : "127.0.0.1", proxyPort ?? 1883)
                    .AddClient(client =>
                    {
                        client.WithClientId(ClientId)
                            .RequestPersistentSession(TimeSpan.FromMinutes(5))
                            .Consume<BatchMessage>(endpoint => endpoint
                                .ConsumeFrom(Topic)
                                .WithQualityOfServiceLevel(qualityOfServiceLevel)
                                .EnableBatchProcessing(batchSize, maxWaitTime)
                                .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader()));

                        configureClient?.Invoke(client);
                    }))
                .AddDelegateSubscriber<IAsyncEnumerable<BatchMessage>>(batches.HandleAsync);
            builder.Services.AddSingleton<IMqttNetClientFactory>(_clientFactory);

            _host = builder.Build();
        }

        public string ClientId { get; } = "stress-batch-" + Guid.NewGuid().ToString("N");

        public string Topic => ClientId;

        public RecordingLoggerProvider Logs { get; } = new();

        public MqttClientRecorder Recorder => _clientFactory.GetRecorder(ClientId);

        private MqttConsumer Consumer => _consumer ?? throw new InvalidOperationException("The consumer isn't started.");

        private IBrokerClientsConnector Connector => _host.Services.GetRequiredService<IBrokerClientsConnector>();

        public async Task StartAsync()
        {
            await Connector.InitializeAsync();
            _consumer = _host.Services.GetRequiredService<IConsumerCollection>().OfType<MqttConsumer>().Single();
            _consumer.Client.Subscribed.AddHandler(_ =>
            {
                _subscribed.TrySetResult(true);

                return ValueTask.CompletedTask;
            });

            await Connector.ConnectAsync().AsTask().WaitAsync(Timeout);
            await _subscribed.Task.WaitAsync(Timeout);
            await _publisher.ConnectAsync(new MqttClientOptionsBuilder()
                .WithTcpServer(MqttFixture.BrokerHost)
                .WithClientId(ClientId + "-publisher")
                .Build()).WaitAsync(Timeout);
            await EmqxCluster.WaitUntilRoutedAsync(Topic, Timeout, ClientId);
        }

        public async Task PublishAsync(int from, int to)
        {
            for (int number = from; number <= to; number++)
            {
                await _publisher.PublishAsync(new MqttApplicationMessageBuilder()
                    .WithTopic(Topic)
                    .WithPayload(JsonSerializer.SerializeToUtf8Bytes(new BatchMessage { Number = number }))
                    .WithQualityOfServiceLevel(_qualityOfServiceLevel)
                    .Build()).WaitAsync(Timeout);
            }
        }

        // Disconnects the client, keeping its persistent session
        public Task DisconnectAsync() => Consumer.Client.DisconnectAsync().AsTask().WaitAsync(Timeout);

        public async Task ReconnectAsync()
        {
            _subscribed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await Consumer.Client.ConnectAsync().AsTask().WaitAsync(Timeout);
            await WaitUntilSubscribedAsync();
        }

        // Cuts the connection and waits until the client reconnects
        public Task LoseConnectionAsync(DisconnectableTcpProxy proxy)
        {
            _subscribed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            proxy.DropConnections();

            return WaitUntilSubscribedAsync();
        }

        // The numbers of the messages delivered with the specified connection, in the order they were received
        public int[] GetDeliveredNumbers(int connection) =>
        [
            .. Recorder.GetDeliveries()
                .Where(delivery => delivery.Connection == connection)
                .Select(delivery => GetNumber(delivery.Payload))
        ];

        // The numbers of the messages acknowledged with the current connection, in the order they were acknowledged
        public int[] GetAcknowledgedNumbers()
        {
            MqttClientRecorder recorder = Recorder;
            int connection = recorder.Connections;
            Dictionary<ushort, int> numbers = [];

            foreach (MqttClientRecorder.Delivery delivery in recorder.GetDeliveries().Where(delivery => delivery.Connection == connection))
            {
                numbers[delivery.PacketIdentifier] = GetNumber(delivery.Payload);
            }

            return
            [
                .. recorder.GetAcknowledgments()
                    .Where(acknowledgment => acknowledgment.Connection == connection)
                    .Select(acknowledgment => numbers.GetValueOrDefault(acknowledgment.PacketIdentifier))
            ];
        }

        // Waits until the specified number of deliveries was received with the current connection and each of them was acknowledged
        // exactly once (and completed, with QoS 2)
        public Task WaitUntilAcknowledgedAsync(int count) =>
            WaitUntilAsync(() => IsAcknowledged(count), $"{count} deliveries to be acknowledged");

        // Disconnects the client and resumes its session with a native client: the broker must not replay any delivery
        public async Task VerifyNoUnacknowledgedDeliveriesAsync()
        {
            await DisconnectAsync();

            EmqxCluster.SessionReplay replay = await EmqxCluster.ResumeAndEndSessionAsync(ClientId, Topic, Timeout);

            replay.IsSessionPresent.ShouldBeTrue();
            replay.Payloads.Select(GetNumber).ShouldBeEmpty();
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await Connector.DisconnectAsync().AsTask().WaitAsync(Timeout);

                if (_publisher.IsConnected)
                    await _publisher.DisconnectAsync().WaitAsync(Timeout);
            }
            finally
            {
                try
                {
                    // Delete the test session, unless the verification ended it already
                    await EmqxCluster.DeleteSessionAsync(ClientId, Timeout);
                }
                finally
                {
                    _output.WriteLine($"Client={ClientId}; batches: {_batches}");
                    WriteRecorderTo(_output);
                    Logs.WriteTo(_output);

                    _host.Dispose();
                    _clientFactory.Dispose();
                    _publisher.Dispose();
                    Logs.Dispose();
                }
            }
        }

        // The routes must be replicated again when the session moved to the other node
        private async Task WaitUntilSubscribedAsync()
        {
            await _subscribed.Task.WaitAsync(Timeout);
            await EmqxCluster.WaitUntilRoutedAsync(Topic, Timeout, ClientId);
        }

        private bool IsAcknowledged(int count)
        {
            MqttClientRecorder recorder = Recorder;
            int connection = recorder.Connections;
            ushort[] delivered = [.. recorder.GetDeliveries().Where(item => item.Connection == connection).Select(item => item.PacketIdentifier)];
            ushort[] acknowledged = [.. recorder.GetAcknowledgments().Where(item => item.Connection == connection).Select(item => item.PacketIdentifier)];

            if (delivered.Length != count || acknowledged.Length != count || !delivered.Order().SequenceEqual(acknowledged.Order()))
                return false;

            if (_qualityOfServiceLevel != MqttQualityOfServiceLevel.ExactlyOnce)
                return true;

            ushort[] completed = [.. recorder.GetCompletions().Where(item => item.Connection == connection).Select(item => item.PacketIdentifier)];

            return delivered.Order().SequenceEqual(completed.Order());
        }

        private void WriteRecorderTo(ITestOutputHelper output)
        {
            if (_clientFactory.FindRecorder(ClientId) is not { } recorder)
                return;

            output.WriteLine(
                $"Connections={recorder.Connections}; disconnections={recorder.Disconnections}; " +
                $"session present={recorder.IsSessionPresent}; max in flight={recorder.MaxInFlight}");
            output.WriteLine(
                "Deliveries: " +
                string.Join(", ", recorder.GetDeliveries().Select(delivery =>
                    $"{GetNumber(delivery.Payload)} (connection {delivery.Connection}, id {delivery.PacketIdentifier}" +
                    $"{(delivery.IsDuplicate ? ", dup" : string.Empty)})")));
            output.WriteLine(
                "Acknowledgments: " +
                string.Join(", ", recorder.GetAcknowledgments().Select(item => $"{item.PacketIdentifier} (connection {item.Connection})")));
        }
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
