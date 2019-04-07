// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Connectors.Repositories
{
    public interface IOffsetStore
    {
        void Store(IOffset offset);
        
        void Commit();

        void Rollback();

        IOffset GetLatestValue(string key);
    }
}