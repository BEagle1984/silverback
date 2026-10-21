// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dekaf.Consumer;
using Dekaf.Errors;
using Dekaf.Producer;
using Silverback.Diagnostics;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     Wraps the Dekaf producer and translates between the Dekaf and the Silverback types.
/// </summary>
/// <remarks>
///     Dekaf models a transaction as an object rather than as producer state, the wrapper therefore keeps the
///     transaction in progress and routes the produced messages through it. The transaction is only released once it
///     has been successfully committed or aborted, so that a failed commit can still be aborted and a failed abort
///     retried, as with the Confluent.Kafka producer.
/// </remarks>
internal sealed class DekafProducerWrapper : BrokerClient, IDekafProducerWrapper
{
    private readonly IDekafClientsFactory _clientsFactory;

    private readonly ISilverbackLogger<DekafProducerWrapper> _logger;

    // The messages produced in the current transaction via the callback-based Produce, which must be appended to the
    // transaction before it is committed or aborted
    private readonly List<Task> _pendingTransactionalProduces = [];

    private readonly System.Threading.Lock _pendingTransactionalProducesLock = new();

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Disposed in DisconnectCoreAsync")]
    private IKafkaProducer<byte[], byte[]>? _dekafProducer;

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Disposed when the transaction ends or the producer is disposed")]
    private ITransaction<byte[], byte[]>? _transaction;

    private bool _hasInitializedTransactions;

    public DekafProducerWrapper(
        string name,
        IDekafClientsFactory clientsFactory,
        KafkaProducerConfiguration configuration,
        ISilverbackLogger<DekafProducerWrapper> logger)
        : base(name, logger)
    {
        _clientsFactory = Check.NotNull(clientsFactory, nameof(clientsFactory));
        Configuration = Check.NotNull(configuration, nameof(configuration));
        _logger = Check.NotNull(logger, nameof(logger));
    }

    public KafkaProducerConfiguration Configuration { get; }

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Life cycle externally handled")]
    public KafkaProducer Producer
    {
        get => field ?? throw new InvalidOperationException("The producer is not initialized yet.");
        set => field = Check.NotNull(value, nameof(value));
    }

    public void Produce(
        TopicPartition topicPartition,
        byte[]? key,
        byte[]? value,
        IReadOnlyCollection<MessageHeader> headers,
        Action<TopicPartitionOffset> onDelivered,
        Action<Exception> onFailed)
    {
        Check.NotNull(onDelivered, nameof(onDelivered));
        Check.NotNull(onFailed, nameof(onFailed));

        IKafkaProducer<byte[], byte[]> dekafProducer = EnsureConnected();
        ITransaction<byte[], byte[]>? transaction = EnsureTransactionIfTransactional();
        ProducerMessage<byte[], byte[]> message = CreateMessage(topicPartition, key, value, headers);

        try
        {
            if (transaction != null)
            {
                // A transaction has no fire-and-forget API, the delivery is awaited in the background instead
                Task produceTask = ProduceInTransactionAndReportAsync(transaction, message, onDelivered, onFailed);

                lock (_pendingTransactionalProducesLock)
                {
                    _pendingTransactionalProduces.Add(produceTask);
                }

                return;
            }

            // FireAsync completes when the message is enqueued, the callback is invoked when it is acknowledged
            dekafProducer.FireAsync(
                    message,
                    (metadata, exception) =>
                    {
                        if (exception != null)
                            onFailed.Invoke(exception);
                        else
                            onDelivered.Invoke(metadata.ToTopicPartitionOffset());
                    })
                .SafeWait();
        }
        catch (KafkaException)
        {
            DisposeOnExceptionIfEnabled();
            throw;
        }
    }

    public async Task<TopicPartitionOffset> ProduceAsync(
        TopicPartition topicPartition,
        byte[]? key,
        byte[]? value,
        IReadOnlyCollection<MessageHeader> headers,
        CancellationToken cancellationToken)
    {
        IKafkaProducer<byte[], byte[]> dekafProducer = EnsureConnected();
        ITransaction<byte[], byte[]>? transaction = EnsureTransactionIfTransactional();
        ProducerMessage<byte[], byte[]> message = CreateMessage(topicPartition, key, value, headers);

        try
        {
            RecordMetadata metadata = transaction != null
                ? await transaction.ProduceAsync(message, cancellationToken).ConfigureAwait(false)
                : await dekafProducer.ProduceAsync(message, cancellationToken).ConfigureAwait(false);

            return metadata.ToTopicPartitionOffset();
        }
        catch (KafkaException)
        {
            DisposeOnExceptionIfEnabled();
            throw;
        }
    }

    public void InitTransactions()
    {
        IKafkaProducer<byte[], byte[]> dekafProducer = EnsureConnected();

        if (_hasInitializedTransactions)
            return;

        try
        {
            WaitWithTimeout(dekafProducer.InitTransactionsAsync, Configuration.TransactionsInitTimeout, "transactions initialization");
            _logger.LogTransactionsInitialized(this);
            _hasInitializedTransactions = true;
        }
        catch (KafkaException)
        {
            DisposeOnExceptionIfEnabled();
            throw;
        }
    }

    public void BeginTransaction()
    {
        IKafkaProducer<byte[], byte[]> dekafProducer = EnsureConnected();

        if (_transaction != null)
            throw new InvalidOperationException("A transaction is already in progress.");

        try
        {
            _transaction = dekafProducer.BeginTransaction();
            _hasInitializedTransactions = true;
            _logger.LogTransactionStarted(this);
        }
        catch (KafkaException)
        {
            DisposeOnExceptionIfEnabled();
            throw;
        }
    }

    public void CommitTransaction()
    {
        ITransaction<byte[], byte[]> transaction = EnsureTransaction();

        try
        {
            WaitWithTimeout(
                async cancellationToken =>
                {
                    await WaitPendingTransactionalProducesAsync(cancellationToken).ConfigureAwait(false);
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                },
                Configuration.TransactionCommitTimeout,
                "transaction commit");
        }
        catch (KafkaException)
        {
            DisposeOnExceptionIfEnabled();
            throw;
        }

        _logger.LogTransactionCommitted(this);
        EndTransaction(transaction);
    }

    public void AbortTransaction()
    {
        ITransaction<byte[], byte[]> transaction = EnsureTransaction();

        try
        {
            WaitWithTimeout(
                async cancellationToken =>
                {
                    await WaitPendingTransactionalProducesAsync(cancellationToken).ConfigureAwait(false);
                    await transaction.AbortAsync(cancellationToken).ConfigureAwait(false);
                },
                Configuration.TransactionAbortTimeout,
                "transaction abort");
        }
        catch (KafkaException)
        {
            DisposeOnExceptionIfEnabled();
            throw;
        }

        _logger.LogTransactionAborted(this);
        EndTransaction(transaction);
    }

    public void SendOffsetsToTransaction(IReadOnlyCollection<TopicPartitionOffset> offsets, ConsumerGroupMetadata groupMetadata)
    {
        Check.NotNull(offsets, nameof(offsets));

        ITransaction<byte[], byte[]> transaction = EnsureTransaction();

        try
        {
            WaitWithTimeout(
                cancellationToken => transaction.SendOffsetsToTransactionAsync(
                    offsets.Select(offset => offset.ToDekaf()),
                    groupMetadata,
                    cancellationToken),
                Configuration.SendOffsetToTransactionTimeout,
                "offsets sending");

            foreach (TopicPartitionOffset offset in offsets)
            {
                _logger.LogOffsetSentToTransaction(this, offset);
            }
        }
        catch (KafkaException)
        {
            DisposeOnExceptionIfEnabled();
            throw;
        }
    }

    protected override async ValueTask ConnectCoreAsync()
    {
        IKafkaProducer<byte[], byte[]> dekafProducer = _clientsFactory.CreateProducer(Configuration);
        _dekafProducer = dekafProducer;

        try
        {
            await dekafProducer.InitializeAsync().ConfigureAwait(false);
        }
        catch
        {
            // A retried connection creates a new client, this one would otherwise keep its background tasks running
            _dekafProducer = null;
            await dekafProducer.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    protected override async ValueTask DisconnectCoreAsync()
    {
        IKafkaProducer<byte[], byte[]>? dekafProducer = _dekafProducer;

        if (dekafProducer == null)
            return;

        try
        {
            using CancellationTokenSource cancellationTokenSource = new(Configuration.FlushTimeout);
            await dekafProducer.FlushAsync(cancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Like the Confluent.Kafka Flush, the messages not delivered within the timeout are given up on
        }
        finally
        {
            await DisposeDekafProducerAsync().ConfigureAwait(false);
        }
    }

    private static ProducerMessage<byte[], byte[]> CreateMessage(
        TopicPartition topicPartition,
        byte[]? key,
        byte[]? value,
        IReadOnlyCollection<MessageHeader> headers) =>
        new()
        {
            Topic = topicPartition.Topic,
            Partition = topicPartition.Partition == Partition.Any ? null : topicPartition.Partition.Value,
            Key = key,

            // A null value is produced as a tombstone
            Value = value!,
            Headers = headers.Count > 0 ? headers.ToDekafHeaders() : null
        };

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception forwarded to the callback")]
    private static async Task ProduceInTransactionAndReportAsync(
        ITransaction<byte[], byte[]> transaction,
        ProducerMessage<byte[], byte[]> message,
        Action<TopicPartitionOffset> onDelivered,
        Action<Exception> onFailed)
    {
        RecordMetadata metadata;

        try
        {
            metadata = await transaction.ProduceAsync(message).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            onFailed.Invoke(ex);
            return;
        }

        onDelivered.Invoke(metadata.ToTopicPartitionOffset());
    }

    // Dekaf signals the timeouts passed as cancellation tokens with an OperationCanceledException, while the
    // Confluent.Kafka producer throws a KafkaException that is handled like any other client error (e.g. by
    // DisposeOnException)
    private static void WaitWithTimeout(Func<CancellationToken, ValueTask> operation, TimeSpan timeout, string operationName)
    {
        using CancellationTokenSource cancellationTokenSource = new(timeout);

        try
        {
            operation.Invoke(cancellationTokenSource.Token).SafeWait();
        }
        catch (OperationCanceledException ex) when (cancellationTokenSource.IsCancellationRequested)
        {
            throw new KafkaTimeoutException($"The {operationName} didn't complete within {timeout}.", ex);
        }
    }

    // The background produce tasks never throw, the outcome is reported to the callbacks
    private async ValueTask WaitPendingTransactionalProducesAsync(CancellationToken cancellationToken)
    {
        Task[] pendingProduces;

        lock (_pendingTransactionalProducesLock)
        {
            pendingProduces = [.. _pendingTransactionalProduces];
        }

        await Task.WhenAll(pendingProduces).WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private void EndTransaction(ITransaction<byte[], byte[]> transaction)
    {
        lock (_pendingTransactionalProducesLock)
        {
            _pendingTransactionalProduces.Clear();
        }

        if (_transaction == transaction)
            _transaction = null;

        transaction.DisposeAsync().SafeWait();
    }

    private IKafkaProducer<byte[], byte[]> EnsureConnected()
    {
        if (Status != ClientStatus.Initialized)
            throw new InvalidOperationException("The producer is not connected.");

        return _dekafProducer ?? throw new InvalidOperationException("The underlying producer is not initialized.");
    }

    private ITransaction<byte[], byte[]> EnsureTransaction()
    {
        EnsureConnected();

        return _transaction ?? throw new InvalidOperationException("No transaction in progress.");
    }

    // Dekaf would silently produce outside of any transaction, while librdkafka rejects it
    private ITransaction<byte[], byte[]>? EnsureTransactionIfTransactional()
    {
        if (_transaction == null && Configuration.IsTransactional)
            throw new InvalidOperationException("The transactional producer can only produce within a transaction. Begin a transaction first.");

        return _transaction;
    }

    private void DisposeOnExceptionIfEnabled()
    {
        if (Configuration.DisposeOnException)
            DisposeDekafProducerAsync().SafeWait();
    }

    private async ValueTask DisposeDekafProducerAsync()
    {
        IKafkaProducer<byte[], byte[]>? dekafProducer = _dekafProducer;
        ITransaction<byte[], byte[]>? transaction = _transaction;
        _dekafProducer = null;
        _transaction = null;
        _hasInitializedTransactions = false;

        lock (_pendingTransactionalProducesLock)
        {
            _pendingTransactionalProduces.Clear();
        }

        if (transaction != null)
            await transaction.DisposeAsync().ConfigureAwait(false);

        if (dekafProducer != null)
            await dekafProducer.DisposeAsync().ConfigureAwait(false);
    }
}
