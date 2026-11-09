// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Messaging.Sequences.Unbounded;

/// <summary>
///     This isn't a real sequence but it's used to handle the stream pushed with all messages not actually
///     belonging to a sequence.
/// </summary>
/// <remarks>
///     Each message is committed by its own pipeline, as soon as the subscribers pull the next one. The <see cref="ISequence.Context" />
///     is the context of the message that started the sequence, which is disposed by its own pipeline.
/// </remarks>
internal sealed class UnboundedSequence : Sequence
{
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Disposed in DisposeContext, called by Dispose")]
    private readonly IDisposable? _serviceScopeLease;

    [SuppressMessage("", "CA2000", Justification = "Stream disposed in base class")]
    public UnboundedSequence(string sequenceId, ConsumerPipelineContext context)
        : base(sequenceId, context, false, trackIdentifiers: false)
    {
        // The subscribers are resolved from the scope of the message that starts the sequence and keep using it until the sequence
        // ends, while the pipeline of that message disposes its context as soon as it completes
        _serviceScopeLease = context.LeaseServiceScope();
    }

    // The transaction of the message that started the sequence is usually committed already: let the pipeline of the message being
    // pushed apply the error policies and roll back
    private protected override bool AppliesErrorPoliciesOnAbort => false;

    // The context is disposed by the pipeline of its message: just release the scope
    private protected override void DisposeContext() => _serviceScopeLease?.Dispose();
}
