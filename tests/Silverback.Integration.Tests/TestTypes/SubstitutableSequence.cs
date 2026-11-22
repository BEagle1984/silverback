// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Subscribers;

namespace Silverback.Tests.Integration.TestTypes;

// A sequence implementing the internal ISequenceImplementation interface, like the actual sequences (public, to be substituted)
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Substituted")]
[SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "Substituted")]
[SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "Substituted")]
[SuppressMessage("Security", "CA2119:Seal methods that satisfy private interfaces", Justification = "Overridden by the substitute")]
public abstract class SubstitutableSequence : ISequenceImplementation
{
    public abstract string SequenceId { get; }

    public abstract int Length { get; }

    public abstract int? TotalLength { get; }

    public abstract bool IsNew { get; }

    public abstract bool IsPending { get; }

    public abstract bool IsCompleting { get; }

    public abstract bool IsComplete { get; }

    public abstract bool IsAborted { get; }

    public abstract bool IsBeingConsumed { get; }

    public abstract SequenceAbortReason AbortReason { get; }

    public abstract Exception? AbortException { get; }

    public abstract IReadOnlyCollection<ISequence> Sequences { get; }

    public abstract ISequence? ParentSequence { get; }

    public abstract ConsumerPipelineContext Context { get; }

    public abstract IMessageStreamProvider StreamProvider { get; }

    public abstract Task SequencerBehaviorsTask { get; }

    public abstract Task ProcessingCompletedTask { get; }

    public abstract IMessageStreamEnumerable<TMessage> CreateStream<TMessage>(IReadOnlyCollection<IMessageFilter>? filters = null);

    public abstract Task<AddToSequenceResult> AddAsync(IRawInboundEnvelope envelope, ISequence? sequence, bool throwIfUnhandled);

    public abstract Task AbortIfIncompleteAsync();

    public abstract Task AbortAsync(SequenceAbortReason reason, Exception? exception = null);

    public abstract IReadOnlyCollection<IBrokerMessageIdentifier> GetCommitIdentifiers();

    public abstract IReadOnlyCollection<IBrokerMessageIdentifier> GetRollbackIdentifiers();

    public abstract void SetIsNew(bool value);

    public abstract void SetParentSequence(ISequence? parentSequence);

    public abstract void CompleteSequencerBehaviorsTask();

    public abstract void NotifyProcessingCompleted();

    public abstract void NotifyProcessingFailed(Exception exception);

    public abstract Task AbortWithParentAsync(SequenceAbortReason reason, Exception? exception);

    public abstract Task AbortPendingChildSequencesAsync();

    public abstract void Dispose();
}
