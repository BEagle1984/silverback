// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Transactions;

internal sealed class KafkaTransaction : IKafkaTransaction
{
    private readonly SilverbackContext _context;

    private IConfluentProducerWrapper? _confluentProducer;

    private bool _isPending;

    public KafkaTransaction(SilverbackContext context, string? transactionalIdSuffix = null)
    {
        _context = Check.NotNull(context, nameof(context));
        TransactionalIdSuffix = transactionalIdSuffix;

        _context.AddKafkaTransaction(this);
    }

    public string? TransactionalIdSuffix { get; }

    public void Commit()
    {
        if (_confluentProducer == null)
            throw new InvalidOperationException("The transaction is not bound to a producer.");

        EnsureIsPending();

        _confluentProducer.CommitTransaction();
        _isPending = false;
        _confluentProducer = null;
    }

    public void Abort()
    {
        EnsureIsPending();

        _confluentProducer?.AbortTransaction();
        _isPending = false;
        _confluentProducer = null;
    }

    public void Dispose()
    {
        if (_isPending)
            Abort();

        _context.RemoveKafkaTransaction();
    }

    internal void EnsureBegin()
    {
        if (_confluentProducer == null)
            throw new InvalidOperationException("The transaction is not bound to a producer.");

        if (!_isPending)
            Begin();
    }

    internal void Begin()
    {
        if (_confluentProducer == null)
            throw new InvalidOperationException("The transaction is not bound to a producer.");

        _confluentProducer.BeginTransaction();
        _isPending = true;
    }

    internal void BindConfluentProducer(IConfluentProducerWrapper confluentProducer)
    {
        if (_confluentProducer != null && confluentProducer != _confluentProducer)
            throw new InvalidOperationException("The transaction is already bound to a producer.");

        _confluentProducer = confluentProducer;
    }

    private void EnsureIsPending()
    {
        if (!_isPending)
            throw new InvalidOperationException("The transaction is not pending.");
    }
}
