// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming.Transaction;
using Silverback.Util;

namespace Silverback.Messaging.Consuming.KafkaOffsetStore;

/// <summary>
///     Creates the <see cref="KafkaOffsetStoreScope" /> and ensures that the offsets are being stored to the client side offset store
///     (if configured), when the consumer is committed after processing the message and when the error policies skip or move it.
/// </summary>
/// <remarks>
///     It runs right after the <see cref="TransactionHandlerConsumerBehavior" />, since the message can fail (e.g., when it cannot be
///     deserialized) or be committed without being processed (e.g., a duplicated chunk) before reaching the subscribers.
/// </remarks>
public class KafkaOffsetStoreConsumerBehavior : IConsumerBehavior
{
    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.TransactionHandler + 1;

    /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
    public ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        if (context.Consumer is KafkaConsumer { Configuration.ClientSideOffsetStore: not null } kafkaConsumer)
        {
            KafkaOffsetStoreScope offsetStoreScope = CreateOffsetStoreScope(context, kafkaConsumer.Configuration.ClientSideOffsetStore);

            // The handlers are bound to the scope of this context, which stores the offsets of its transaction. The scope set in the
            // ISilverbackContext is the one of the current async flow instead, which belongs to another message when the transaction is
            // completed by another pipeline (e.g., an incomplete chunked message aborted by the next message).
            context.SilverbackContext.SetKafkaOffsetStoreScope(offsetStoreScope);
            context.TransactionManager.Committing.AddHandler(committingContext => StoreOffsetsAsync(offsetStoreScope, committingContext));
            context.TransactionManager.Skipping.AddHandler(skippedContext => StoreSkippedOffsetsAsync(offsetStoreScope, skippedContext));
        }

        return next(context, cancellationToken);
    }

    private static KafkaOffsetStoreScope CreateOffsetStoreScope(ConsumerPipelineContext context, KafkaOffsetStoreSettings storeSettings) =>
        new(
            context.ServiceProvider.GetRequiredService<IKafkaOffsetStoreFactory>().GetStore(storeSettings, context.ServiceProvider),
            context);

    // The offsets are stored in the storage transaction enlisted by the subscriber, if any. The message that doesn't belong to a sequence
    // and is committed without being published (e.g., a duplicated chunk skipped by the sequencer) is committed by the transaction
    // handler even while other sequences are pending or unresolved (e.g., the batch that a chunked message was added to): its offset
    // isn't stored then, since it would skip their messages after the restart, but by the next commit.
    private static ValueTask StoreOffsetsAsync(KafkaOffsetStoreScope offsetStoreScope, ConsumerPipelineContext context)
    {
        if (context.Sequence == null && !IsPublished(context) &&
            context.TransactionManager is ConsumerTransactionManager transactionManager &&
            !transactionManager.CanCommitUnprocessedMessage())
        {
            return ValueTask.CompletedTask;
        }

        return new ValueTask(offsetStoreScope.StoreOffsetsAsync());
    }

    // The pipeline steps are never stepped back, so the current one is the last step that the message reached
    private static bool IsPublished(ConsumerPipelineContext context) =>
        context.CurrentStepIndex >= context.Pipeline.Count ||
        context.Pipeline[context.CurrentStepIndex].SortIndex >= BrokerBehaviorsSortIndexes.Consumer.Publisher;

    // The storage transaction enlisted by the subscriber (if any) failed together with the message: the offsets are stored outside it. A
    // failure is retried once right away and then logged, letting the error policy complete: the message was already skipped or moved and
    // failing would skip or move it again after the restart, while the stored offset is otherwise updated by the next processed message.
    // There's no cancellation to observe: the store isn't canceled by the consumer stop, and a cancellation exception thrown by the store
    // (e.g., a command timeout) is a store failure like any other.
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Retried once and then logged")]
    private static async ValueTask StoreSkippedOffsetsAsync(KafkaOffsetStoreScope offsetStoreScope, ConsumerPipelineContext context)
    {
        try
        {
            await offsetStoreScope.StoreSkippedOffsetsAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            try
            {
                await offsetStoreScope.StoreSkippedOffsetsAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                context.ServiceProvider.GetRequiredService<ISilverbackLogger<KafkaOffsetStoreConsumerBehavior>>()
                    .LogSkippedOffsetsStoreError(offsetStoreScope, (KafkaConsumer)context.Consumer, exception);
            }
        }
    }
}
