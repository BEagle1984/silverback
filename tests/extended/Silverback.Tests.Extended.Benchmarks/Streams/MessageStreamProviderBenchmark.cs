// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Silverback.Messaging.Messages;
using Silverback.Tests.Extended.Benchmarks.Common;

namespace Silverback.Tests.Extended.Benchmarks.Streams;

/// <summary>
///     Measures the whole lifecycle of a <c>MessageStreamProvider</c> (the way the sequences use it): the streams are created, the
///     subscribers start enumerating them, the messages are pushed and the streams are completed.
/// </summary>
[Config(typeof(MicroBenchmarkConfig))]
[MemoryDiagnoser]
public class MessageStreamProviderBenchmark
{
    private static readonly StreamMessage Message = new();

    [Params(1, 3)]
    public int StreamsCount { get; set; }

    [Params(1, 100)]
    public int MessagesCount { get; set; }

    [Benchmark]
    public async Task<int> PushAndEnumerateAsync()
    {
        using MessageStreamProvider<StreamMessage> provider = new();

        Task<int>[] subscribers = new Task<int>[StreamsCount];

        for (int i = 0; i < StreamsCount; i++)
        {
            IMessageStreamEnumerable<StreamMessage> stream = provider.CreateStream<StreamMessage>();
            subscribers[i] = EnumerateAsync(stream);
        }

        await PushAllAsync(provider);

        int[] counts = await Task.WhenAll(subscribers);
        return counts[0];
    }

    [Benchmark]
    public async Task<int> PushAndEnumerateSync()
    {
        using MessageStreamProvider<StreamMessage> provider = new();

        Task<int>[] subscribers = new Task<int>[StreamsCount];

        for (int i = 0; i < StreamsCount; i++)
        {
            IMessageStreamEnumerable<StreamMessage> stream = provider.CreateStream<StreamMessage>();
            subscribers[i] = Task.Run(() => Enumerate(stream));
        }

        await PushAllAsync(provider);

        int[] counts = await Task.WhenAll(subscribers);
        return counts[0];
    }

    private static async Task<int> EnumerateAsync(IAsyncEnumerable<StreamMessage> stream)
    {
        // Yield so that the subscriber doesn't run synchronously in the loop creating the streams
        await Task.Yield();

        int count = 0;

        await foreach (StreamMessage message in stream)
        {
            count += message.Value;
        }

        return count;
    }

    private static int Enumerate(IEnumerable<StreamMessage> stream)
    {
        int count = 0;

        foreach (StreamMessage message in stream)
        {
            count += message.Value;
        }

        return count;
    }

    private async Task PushAllAsync(MessageStreamProvider<StreamMessage> provider)
    {
        for (int i = 0; i < MessagesCount; i++)
        {
            await provider.PushAsync(Message, cancellationToken: CancellationToken.None);
        }

        await provider.CompleteAsync();
    }

    private sealed class StreamMessage
    {
        public int Value { get; } = 1;
    }
}
