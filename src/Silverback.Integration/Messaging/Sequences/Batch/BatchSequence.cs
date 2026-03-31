// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Sequences.Batch;

/// <summary>
///     Represent an arbitrary sequence of messages created to consume unrelated messages in batch (see <see cref="BatchSettings" />).
/// </summary>
public class BatchSequence : Sequence
{
    private readonly ISilverbackLogger<BatchSequence> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BatchSequence" /> class.
    /// </summary>
    /// <param name="sequenceId">
    ///     The identifier that is used to match the consumed messages with their belonging sequence.
    /// </param>
    /// <param name="context">
    ///     The current <see cref="ConsumerPipelineContext" />, assuming that it will be the one from which the sequence gets published
    ///     via the message bus.
    /// </param>
    public BatchSequence(string sequenceId, ConsumerPipelineContext context)
        : base(
            sequenceId,
            context,
            Check.NotNull(context, nameof(context)).Envelope.Endpoint.Configuration.Batch?.MaxWaitTime != null,
            Check.NotNull(context, nameof(context)).Envelope.Endpoint.Configuration.Batch?.MaxWaitTime)
    {
        if (context.Envelope.Endpoint.Configuration.Batch == null)
            throw new InvalidOperationException("Endpoint.Batch is null.");

        TotalLength = context.Envelope.Endpoint.Configuration.Batch.Size;

        _logger = context.ServiceProvider.GetRequiredService<ISilverbackLogger<BatchSequence>>();
    }

    /// <summary>
    ///     Called when the timeout is elapsed. In this special case the sequence is completed instead of aborted.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    protected override async Task OnTimeoutElapsedAsync()
    {
        try
        {
            await CompleteAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogSequenceTimeoutError(this, ex);

            await AbortAsync(SequenceAbortReason.Error, ex).ConfigureAwait(false);
        }
    }
}
