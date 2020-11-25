// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Inbound.Transaction;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Behaviors
{
    /// <summary>
    ///     The context that is passed along the consumer behaviors pipeline.
    /// </summary>
    public sealed class ConsumerPipelineContext : IDisposable
    {
        private IServiceScope? _serviceScope;

        private IConsumerTransactionManager? _transactionManager;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConsumerPipelineContext" /> class.
        /// </summary>
        /// <param name="envelope">
        ///     The envelope containing the message being processed.
        /// </param>
        /// <param name="consumer">
        ///     The <see cref="IConsumer" /> that triggered this pipeline.
        /// </param>
        /// <param name="sequenceStore">
        ///     The <see cref="ISequenceStore" /> used to temporary store the pending sequences being consumed.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
        /// </param>
        public ConsumerPipelineContext(
            IRawInboundEnvelope envelope,
            IConsumer consumer,
            ISequenceStore sequenceStore,
            IServiceProvider serviceProvider)
        {
            Envelope = Check.NotNull(envelope, nameof(envelope));
            Consumer = Check.NotNull(consumer, nameof(consumer));
            SequenceStore = Check.NotNull(sequenceStore, nameof(sequenceStore));
            ServiceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        }

        /// <summary>
        ///     Gets the <see cref="IConsumer" /> that triggered this pipeline.
        /// </summary>
        public IConsumer Consumer { get; }

        /// <summary>
        ///     Gets the identifiers of the messages being handled in this context (either the single message or the
        ///     sequence).
        /// </summary>
        public IReadOnlyCollection<IBrokerMessageIdentifier> BrokerMessageIdentifiers =>
            Sequence?.BrokerMessageIdentifiers ?? new[] { Envelope.BrokerMessageIdentifier };

        /// <summary>
        ///     Gets the <see cref="ISequenceStore" /> used to temporary store the pending sequences being consumed.
        /// </summary>
        public ISequenceStore SequenceStore { get; }

        /// <summary>
        ///     Gets a the <see cref="ISequence" /> the current message belongs to.
        /// </summary>
        public ISequence? Sequence { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether the current message was recognized as the beginning of a new
        ///     sequence.
        /// </summary>
        public bool IsSequenceStart { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether the current message was recognized as the end of the sequence.
        /// </summary>
        public bool IsSequenceEnd { get; private set; }

        /// <summary>
        ///     Gets the <see cref="IServiceProvider" /> to be used to resolve the required services.
        /// </summary>
        public IServiceProvider ServiceProvider { get; private set; }

        /// <summary>
        ///     Gets the <see cref="IConsumerTransactionManager" /> that is handling the current pipeline transaction.
        /// </summary>
        public IConsumerTransactionManager TransactionManager
        {
            get
            {
                if (_transactionManager == null)
                    throw new InvalidOperationException("The transaction manager is not initialized.");

                return _transactionManager;
            }

            internal set
            {
                if (_transactionManager != null)
                    throw new InvalidOperationException("The transaction manager is already initialized.");

                _transactionManager = value;
            }
        }

        /// <summary>
        ///     Gets or sets the envelopes containing the messages being processed.
        /// </summary>
        public IRawInboundEnvelope Envelope { get; set; }

        /// <summary>
        ///     Gets the <see cref="Task" /> representing the message processing when it is not directly awaited (e.g.
        ///     when starting the processing of a <see cref="Sequence" />. This <see cref="Task" /> will complete when
        ///     all subscribers return.
        /// </summary>
        public Task? ProcessingTask { get; internal set; }

        /// <summary>
        ///     Replaces the <see cref="IServiceProvider" /> with the one from the specified scope.
        /// </summary>
        /// <param name="newServiceScope">
        ///     The <see cref="IServiceScope" /> to be used.
        /// </param>
        public void ReplaceServiceScope(IServiceScope newServiceScope)
        {
            _serviceScope?.Dispose();

            _serviceScope = Check.NotNull(newServiceScope, nameof(newServiceScope));
            ServiceProvider = newServiceScope.ServiceProvider;
        }

        /// <summary>
        ///     Sets the current sequence.
        /// </summary>
        /// <param name="sequence">
        ///     The <see cref="ISequence" /> being processed.
        /// </param>
        /// <param name="isSequenceStart">
        ///     A value indicating whether the current message was recognized as the beginning of a new sequence.
        /// </param>
        public void SetSequence(ISequence sequence, in bool isSequenceStart)
        {
            Sequence = sequence;
            IsSequenceStart = isSequenceStart;
        }

        /// <summary>
        ///     Sets the <see cref="IsSequenceEnd" /> property to <c>true</c>, indicating that the current message was
        ///     recognized as the end of the sequence.
        /// </summary>
        public void SetIsSequenceEnd()
        {
            IsSequenceEnd = true;
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            _serviceScope?.Dispose();
            _serviceScope = null;

            if (ProcessingTask == null || !ProcessingTask.IsCompleted)
                return;

            ProcessingTask.Dispose();
            ProcessingTask = null;
        }
    }
}
