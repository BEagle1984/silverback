﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Connectors.Repositories
{
    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
    public class InMemoryOffsetStore : IOffsetStore
    {
        private static readonly Dictionary<string, IComparableOffset> LatestOffsets =
            new Dictionary<string, IComparableOffset>();

        private readonly Dictionary<string, IComparableOffset> _uncommittedOffsets =
            new Dictionary<string, IComparableOffset>();

        public Task Store(IComparableOffset offset)
        {
            lock (_uncommittedOffsets)
            {
                _uncommittedOffsets[offset.Key] = offset;
            }

            return Task.CompletedTask;
        }

        public Task<IComparableOffset> GetLatestValue(string key) =>
            Task.FromResult(
                LatestOffsets.Union(_uncommittedOffsets)
                    .Where(o => o.Key == key)
                    .Select(o => o.Value)
                    .Max());

        public Task Commit()
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

            return Task.CompletedTask;
        }

        public Task Rollback()
        {
            lock (_uncommittedOffsets)
            {
                _uncommittedOffsets.Clear();
            }

            return Task.CompletedTask;
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