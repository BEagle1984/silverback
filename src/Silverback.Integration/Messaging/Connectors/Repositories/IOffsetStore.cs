// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Connectors.Repositories
{
    public interface IOffsetStore : ITransactional
    {
        Task Store(IComparableOffset offset, IConsumerEndpoint endpoint);

        Task<IComparableOffset> GetLatestValue(string offsetKey, IConsumerEndpoint endpoint);
    }
}