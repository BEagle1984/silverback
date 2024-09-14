// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Benchmarks.Latest.Common;

public class AsyncSubscriber<TMessage>
{
    private int _receivedMessagesCount;

    public int ReceivedMessagesCount => _receivedMessagesCount;

    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required for routing")]
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Invoked by Silverback")]
    [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = "Required for routing")]
    public async Task HandleMessageAsync(TMessage message)
    {
        Interlocked.Increment(ref _receivedMessagesCount);

        await Task.Yield();
    }
}
