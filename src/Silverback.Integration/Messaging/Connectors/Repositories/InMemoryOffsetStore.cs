// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Connectors.Repositories
{
    public class InMemoryOffsetStore : IOffsetStore
    {
        private static readonly Dictionary<string, IOffset> LatestOffsets = new Dictionary<string, IOffset>();
        private readonly Dictionary<string, IOffset> _uncommittedOffsets = new Dictionary<string, IOffset>();

        public void Store(IOffset offset)
        {
            lock (_uncommittedOffsets)
            {
                _uncommittedOffsets[offset.Key] = offset;
            }
        }
        public IOffset GetLatestValue(string key) =>
            LatestOffsets.Union(_uncommittedOffsets).Where(o => o.Key == key).Select(o => o.Value).Max();

        public void Commit()
        {
            lock (_uncommittedOffsets)
            {
                lock (LatestOffsets)
                {
                    foreach (var uncommitted in _uncommittedOffsets)
                    {
                        if (!LatestOffsets.ContainsKey(uncommitted.Key) ||
                            LatestOffsets[uncommitted.Key].CompareTo(uncommitted.Value) < 0)
                        {
                            LatestOffsets[uncommitted.Key] = uncommitted.Value;
                        }
                    }
                }

                _uncommittedOffsets.Clear();
            }
        }

        public void Rollback()
        {
            lock (_uncommittedOffsets)
            {
                _uncommittedOffsets.Clear();
            }
        }

        public int Count => LatestOffsets.Count;

        public static void Clear()
        {
            lock (LatestOffsets)
            {
                LatestOffsets.Clear();
            }
        }
    }
}