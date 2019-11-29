// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Silverback.Messaging.Connectors.Repositories
{
    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
    public class TransactionalList<T>
    {
        protected static readonly List<T> Entries = new List<T>();
        protected readonly List<T> UncommittedEntries = new List<T>();

        public Task<int> GetLength() => Task.FromResult(Entries.Count);

        protected Task Add(T entry)
        {
            lock (UncommittedEntries)
            {
                UncommittedEntries.Add(entry);
            }

            return Task.CompletedTask;
        }

        public virtual Task Commit()
        {
            lock (UncommittedEntries)
            {
                lock (Entries)
                {
                    Entries.AddRange(UncommittedEntries);
                }

                UncommittedEntries.Clear();
            }

            return Task.CompletedTask;
        }

        public virtual Task Rollback()
        {
            lock (UncommittedEntries)
            {
                UncommittedEntries.Clear();
            }

            return Task.CompletedTask;
        }

        protected Task Remove(T entry)
        {
            lock (Entries)
            {
                Entries.Remove(entry);
            }

            return Task.CompletedTask;
        }

        public static void Clear()
        {
            lock (Entries)
            {
                Entries.Clear();
            }
        }
    }
}