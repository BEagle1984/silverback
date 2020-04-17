// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Connectors.Repositories
{
    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
    public class InMemoryOffsetStore : TransactionalDictionary<string, IComparableOffset>, IOffsetStore
    {
        public InMemoryOffsetStore(TransactionalDictionarySharedItems<string, IComparableOffset> sharedItems)
            : base(sharedItems)
        {
        }

        public Task Store(IComparableOffset offset, IConsumerEndpoint endpoint)
        {
            AddOrReplace(GetKey(offset.Key, endpoint), offset);

            return Task.CompletedTask;
        }

        public Task<IComparableOffset> GetLatestValue(string offsetKey, IConsumerEndpoint endpoint) =>
            Task.FromResult(
                Items.Union(UncommittedItems)
                    .Where(pair => pair.Key == GetKey(offsetKey, endpoint))
                    .Select(pair => pair.Value)
                    .Max());

        private string GetKey(string offsetKey, IConsumerEndpoint endpoint) =>
            $"{endpoint.GetUniqueConsumerGroupName()}|{offsetKey}";
    }
}