// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Messaging.Broker.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker.Kafka;

public partial class KafkaRebalanceLifecycleTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Callbacks and shutdown complete before the harness is disposed.")]
    public async Task Batch_ShouldPreserveFirstProcessingOrder_WhenShutdownOverflowsAndPartialBatchReplays(bool independent, bool autoCommit)
    {
        await using PollHarness harness = new(independent, backpressure: 2, autoCommit: autoCommit, batchPipeline: true);
        await harness.StartAsync(0);
        await harness.DeliverAsync([.. Enumerable.Range(0, 10).Select(offset => (0, (long)offset))]);
        await PollHarness.WaitUntilAsync(() => harness.StoredOffset(0) == 10);
        harness.Client.Commit();
        harness.CompletedBatchSizes.ShouldBe([10]);

        // Leave a real batch pending, with 17/18 buffered and the native poll blocked writing 19
        ProcessingGate active = harness.Block(0, 16);
        active.IgnoreCancellation = true;
        await harness.DeliverAsync([.. Enumerable.Range(10, 7).Select(offset => (0, (long)offset))]);
        await active.Started.Task.WaitAsync(Timeout);
        await harness.DeliverAsync((0, 17), (0, 18));
        await harness.PollAsync(() => harness.Record(0, 19));
        PartitionChannel channel = harness.GetChannel(0)!;
        TaskCompletionSource<bool> stoppingReader = NewSignal();
        TaskCompletionSource<bool> allowReaderStop = NewSignal();
        TaskCompletionSource<bool> nextProcessed = NewSignal();
        TaskCompletionSource<bool> finishPipeline = NewSignal();
        harness.BeforeChannelStop = () =>
        {
            stoppingReader.TrySetResult(true);
            allowReaderStop.Task.WaitAsync(Timeout).GetAwaiter().GetResult();
        };
        harness.AfterBatchRecord = async delivery =>
        {
            if (delivery.Offset < 17)
                return;
            nextProcessed.TrySetResult(true);
            await finishPipeline.Task.WaitAsync(Timeout);
        };

        Task stop = Task.Run(async () => await harness.Consumer.StopAsync());
        long[] departingProcessed;
        try
        {
            await stoppingReader.Task.WaitAsync(Timeout);
            await harness.PollingStopped.WaitAsync(Timeout);
            active.Release.TrySetResult(true);
            await nextProcessed.Task.WaitAsync(Timeout);
            await PollHarness.WaitUntilAsync(() => harness.Completed.Count == 18);
            departingProcessed = [.. harness.Completed.Select(delivery => delivery.Offset)];
            harness.StoredOffset(0).ShouldBe(10, "An unfinished batch must not advance the stored offset");
            allowReaderStop.TrySetResult(true);
            await PollHarness.WaitUntilAsync(() => channel.ReadCancellationToken.IsCancellationRequested);
        }
        catch (Exception exception)
        {
            _output.WriteLine($"Batch shutdown failed: {exception}");
            _output.WriteLine($"Subscriber order: {string.Join(", ", harness.Completed.Select(delivery => delivery.Offset))}");
            _output.WriteLine(string.Join(Environment.NewLine, harness.Logs.TakeLast(30)));
            throw;
        }
        finally
        {
            allowReaderStop.TrySetResult(true);
            active.Release.TrySetResult(true);
            finishPipeline.TrySetResult(true);
            await stop.WaitAsync(Timeout);
            harness.BeforeChannelStop = null;
            harness.AfterBatchRecord = null;
        }

        long[] expected = [.. Enumerable.Range(0, 20).Select(offset => (long)offset)];
        _output.WriteLine($"Departing subscriber order: {string.Join(", ", departingProcessed)}");
        departingProcessed.ShouldBe(expected.Take(18));
        harness.Client.Commit();
        long replayFrom = harness.CommittedOffset(0);
        replayFrom.ShouldBe(10);
        harness.CompletedBatchSizes.ShouldBe([10]);
        await harness.StartAsync(0);
        for (long offset = replayFrom; offset < 20; offset++)
            await harness.DeliverAsync((0, offset));
        await PollHarness.WaitUntilAsync(() => harness.StoredOffset(0) == 20);
        harness.Client.Commit();

        long[] processed = [.. harness.Completed.Select(delivery => delivery.Offset)];
        _output.WriteLine($"Subscriber order: {string.Join(", ", processed)}; replay starts at {replayFrom}");
        processed.Distinct().ShouldBe(expected, "First processing must remain ordered even across partial-batch replay");
        harness.Completed.Where(delivery => delivery.Epoch == 1).Select(delivery => delivery.Offset).ShouldBe(expected.Take(18));
        harness.Completed.Where(delivery => delivery.Epoch == 2).Select(delivery => delivery.Offset).ShouldBe(expected.Skip(10));
        harness.CompletedBatchSizes.ShouldBe([10, 10]);
        harness.CommittedOffset(0).ShouldBe(20);
        harness.UnsafeStores.ShouldBeEmpty();
        harness.UnsafeCommits.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();
    }
}
