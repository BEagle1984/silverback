// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Util;

namespace Silverback.Messaging.Connectors
{
    public class ConsumerTransactionManager : ISubscriber
    {
        private readonly List<ITransactional> _transactionalServices = new List<ITransactional>();

        public void Enlist(ITransactional transactionalService)
        {
            if (_transactionalServices.Contains(transactionalService))
                return;

            lock (_transactionalServices)
            {
                if (_transactionalServices.Contains(transactionalService))
                    return;

                _transactionalServices.Add(transactionalService);
            }
        }

        [Subscribe, SuppressMessage("ReSharper", "UnusedMember.Global")]
        public Task OnConsumingCompleted(ConsumingCompletedEvent completedEvent) =>
            _transactionalServices.ForEachAsync(transactional => transactional.Commit());

        [Subscribe, SuppressMessage("ReSharper", "UnusedMember.Global")]
        public Task OnConsumingAborted(ConsumingAbortedEvent abortedEvent) =>
            _transactionalServices.ForEachAsync(transactional => transactional.Rollback());
    }
}