// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Connectors.Repositories
{
    /// <summary>
    ///     <para>
    ///         Used by the <see cref="OffsetStoredInboundConnector" /> to keep track of the last processed
    ///         offsets and guarantee that each message is processed only once.
    ///     </para>
    ///     <para>
    ///         The log is simply persisted in memory.
    ///     </para>
    /// </summary>
    public class InMemoryOffsetStore : TransactionalDictionary<string, IComparableOffset>, IOffsetStore
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="InMemoryOffsetStore" /> class.
        /// </summary>
        /// <param name="sharedItems">
        ///     The offsets shared between the instances of this repository.
        /// </param>
        public InMemoryOffsetStore(TransactionalDictionarySharedItems<string, IComparableOffset> sharedItems)
            : base(sharedItems)
        {
        }

        /// <inheritdoc cref="IOffsetStore.StoreAsync" />
        public Task StoreAsync(IComparableOffset offset, IConsumerEndpoint endpoint)
        {
            Check.NotNull(offset, nameof(offset));
            Check.NotNull(endpoint, nameof(endpoint));

            return AddOrReplaceAsync(GetKey(offset.Key, endpoint), offset);
        }

        /// <inheritdoc cref="IOffsetStore.GetLatestValueAsync" />
        public Task<IComparableOffset?> GetLatestValueAsync(string offsetKey, IConsumerEndpoint endpoint) =>
            Task.FromResult(
                (IComparableOffset?)Items.Union(UncommittedItems)
                    .Where(pair => pair.Key == GetKey(offsetKey, endpoint))
                    .Select(pair => pair.Value)
                    .Max());

        private static string GetKey(string offsetKey, IConsumerEndpoint endpoint) =>
            $"{endpoint.GetUniqueConsumerGroupName()}|{offsetKey}";
    }
}
