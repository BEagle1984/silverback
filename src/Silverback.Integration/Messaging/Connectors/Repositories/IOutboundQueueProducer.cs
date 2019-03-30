// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories
{
    public interface IOutboundQueueProducer
    {
        Task Enqueue(IOutboundMessage message);

        Task Commit();

        Task Rollback();
    }
}