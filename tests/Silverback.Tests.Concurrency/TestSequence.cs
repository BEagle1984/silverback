// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;

namespace Silverback.Tests.Concurrency;

// A minimal concrete SequenceBase for Coyote tests. We subclass RawSequence rather than
// BatchSequence/ChunkSequence to avoid pulling in the timeout timer (a fire-and-forget
// Task.Run polling loop) and the chunk-ordering state machine, neither of which is
// relevant to the completion/abort synchronization we want Coyote to explore.
//
// Exposes a couple of reflection-based hooks into SequenceBase<>'s private members so
// tests can reproduce the exact race conditions described in the audit (notably C1,
// where AddCoreAsync calls the private CompleteCoreAsync while holding only
// _addSemaphoreSlim).
internal sealed class TestSequence : RawSequence
{
    private static readonly MethodInfo CompleteCoreAsyncMethod =
        typeof(SequenceBase<IRawInboundEnvelope>)
            .GetMethod("CompleteCoreAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly FieldInfo AddSemaphoreSlimField =
        typeof(SequenceBase<IRawInboundEnvelope>)
            .GetField("_addSemaphoreSlim", BindingFlags.NonPublic | BindingFlags.Instance)!;

    public TestSequence(string sequenceId, ConsumerPipelineContext context, int? totalLength = null)
        : base(sequenceId, context, enforceTimeout: false)
    {
        if (totalLength.HasValue)
        {
            TotalLength = totalLength.Value;
        }
    }

    // Mimics what AddCoreAsync does at its tail when Length == TotalLength: acquire
    // _addSemaphoreSlim and then call the private CompleteCoreAsync directly WITHOUT
    // acquiring _completeSemaphoreSlim. This is the exact race pattern from audit
    // finding C1: the public CompleteAsync wrapper guards against racing with
    // AbortAsync via _completeSemaphoreSlim, but the inlined call from AddCoreAsync
    // bypasses that guard.
    public async Task TriggerAddPathCompleteAsync()
    {
        SemaphoreSlim addSemaphore = (SemaphoreSlim)AddSemaphoreSlimField.GetValue(this)!;
        await addSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            ValueTask task = (ValueTask)CompleteCoreAsyncMethod
                .Invoke(this, new object[] { default(CancellationToken) })!;
            await task.ConfigureAwait(false);
        }
        finally
        {
            addSemaphore.Release();
        }
    }
}
