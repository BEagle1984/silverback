using System.Collections.Generic;

namespace Silverback.Messaging.Repositories
{
    public class TransactionalList<T>
    {
        protected static readonly List<T> Entries = new List<T>();
        private readonly List<T> _uncommittedEntries = new List<T>();

        public int Length => Entries.Count;

        protected void Add(T entry)
        {
            lock (_uncommittedEntries)
            {
                _uncommittedEntries.Add(entry);
            }
        }

        public void Commit()
        {
            lock (_uncommittedEntries)
            {
                lock (Entries)
                {
                    Entries.AddRange(_uncommittedEntries);
                }

                _uncommittedEntries.Clear();
            }
        }

        public void Rollback()
        {
            lock (_uncommittedEntries)
            {
                _uncommittedEntries.Clear();
            }
        }

        protected void Remove(T entry)
        {
            lock (Entries)
            {
                Entries.Remove(entry);
            }
        }

        public void Clear()
        {
            lock (Entries)
            {
                Entries.Clear();
            }
        }
    }
}