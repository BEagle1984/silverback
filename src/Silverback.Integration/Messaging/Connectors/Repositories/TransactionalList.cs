// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Messaging.Connectors.Repositories
{
    public class TransactionalList<T>
    {
        protected static readonly List<T> Entries = new List<T>();
        protected readonly List<T> UncommittedEntries = new List<T>();

        public int Length => Entries.Count;

        protected void Add(T entry)
        {
            lock (UncommittedEntries)
            {
                UncommittedEntries.Add(entry);
            }
        }

        public virtual void Commit()
        {
            lock (UncommittedEntries)
            {
                lock (Entries)
                {
                    Entries.AddRange(UncommittedEntries);
                }

                UncommittedEntries.Clear();
            }
        }

        public virtual void Rollback()
        {
            lock (UncommittedEntries)
            {
                UncommittedEntries.Clear();
            }
        }

        protected void Remove(T entry)
        {
            lock (Entries)
            {
                Entries.Remove(entry);
            }
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