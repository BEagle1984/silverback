// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Messaging.Connectors
{
    public interface IOutboundQueueWorker
    {
        Task ProcessQueue(CancellationToken stoppingToken);
    }
}