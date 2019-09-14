// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Connectors.Repositories
{
    public interface IOffsetStore
    {
        Task Store(IOffset offset);
        
        Task Commit();

        Task Rollback();

        Task<IOffset> GetLatestValue(string key);
    }
}