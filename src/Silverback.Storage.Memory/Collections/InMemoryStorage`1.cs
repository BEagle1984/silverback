// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Silverback.Util;

namespace Silverback.Collections;

/// <summary>
///     This class is used as in-memory storage for various items. The add operations will be execute in the context of the ambient
///     transaction (<see cref="Transaction.Current"/>).
/// </summary>
/// <typeparam name="T">
///    The type of the entities.
/// </typeparam>
public class InMemoryStorage<T>
    where T : class
{
    private readonly List<InMemoryStoredItem<T>> _items = new();

    private readonly ConcurrentDictionary<Transaction, SinglePhaseNotification> _enlistments = new();

    //public IReadOnlyList<InMemoryStorageItem<T>> Items => _items;

    public int ItemsCount
    {
        get
        {
            lock (_items)
            {
                return _items.Count;
            }
        }
    }

    public TimeSpan GetMaxAge()
    {
        lock (_items)
        {
            if (_items.Count == 0)
                return TimeSpan.Zero;

            DateTime oldestInsertDateTime = _items.Min(item => item.InsertDateTime);

            return oldestInsertDateTime == default
                ? TimeSpan.Zero
                : DateTime.UtcNow - oldestInsertDateTime;
        }
    }

    public IReadOnlyCollection<T> Get(int count)
    {
        lock (_items)
        {
            return _items.Take(count).Select(item => item.Item).ToList();
        }
    }

    public void Add(T item)
    {
        InMemoryStoredItem<T> inMemoryStoredItem = new(item);

        if (Transaction.Current == null)
        {
            lock (_items)
            {
                _items.Add(inMemoryStoredItem);
            }
        }
        else
        {
            SinglePhaseNotification singlePhaseNotification = _enlistments.GetOrAdd(
                Transaction.Current,
                static (transaction, args) =>
                {
                    SinglePhaseNotification singlePhaseNotification = new(args.Items, args.Enlistments);
                    transaction.EnlistVolatile(singlePhaseNotification, EnlistmentOptions.None);

                    return singlePhaseNotification;
                },
                (Items: _items, Enlistments: _enlistments));

            singlePhaseNotification.UncommittedItems.Add(inMemoryStoredItem);
        }
    }

    public void Remove(IEnumerable<T> itemsToRemove)
    {
        lock (_items)
        {
            foreach (T itemToRemove in itemsToRemove)
            {
                int index = _items.FindIndex(item => item.Item == itemToRemove);

                if (index >= 0)
                    _items.RemoveAt(index);
            }
        }
    }

    private class SinglePhaseNotification : ISinglePhaseNotification
    {
        private readonly List<InMemoryStoredItem<T>> _items;

        private readonly ConcurrentDictionary<Transaction, SinglePhaseNotification> _enlistments;

        private readonly Transaction _transaction;

        private bool _pending = true;

        public SinglePhaseNotification(
            List<InMemoryStoredItem<T>> items,
            ConcurrentDictionary<Transaction, SinglePhaseNotification> enlistments)
        {
            _items = items;
            _enlistments = enlistments;
            _transaction = Transaction.Current;
        }

        public List<InMemoryStoredItem<T>> UncommittedItems { get; } = new();

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        public void Commit(Enlistment enlistment)
        {
            lock (UncommittedItems)
            {
                if (!_pending)
                    throw new InvalidOperationException("The transaction has already been committed.");

                lock (_items)
                {
                    _items.AddRange(UncommittedItems);
                }

                _enlistments.Remove(_transaction, out _);

                _pending = false;
            }

            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            lock (UncommittedItems)
            {
                if (!_pending)
                    throw new InvalidOperationException("The transaction has already been committed.");

                _pending = false;
            }

            enlistment.Done();
        }

        public void InDoubt(Enlistment enlistment) => Rollback(enlistment); // TODO: WTF?

        public void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment) => Commit(singlePhaseEnlistment);
    }
}
