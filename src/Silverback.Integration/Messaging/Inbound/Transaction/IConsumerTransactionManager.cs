// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;

namespace Silverback.Messaging.Inbound.Transaction
{
    public interface IConsumerTransactionManager
    {
        /// <summary>
        ///     Adds the specified service to the transaction participants to be called upon commit or rollback.
        /// </summary>
        /// <param name="transactionalService">
        ///     The service to be enlisted.
        /// </param>
        void Enlist(ITransactional transactionalService);

        Task Commit();

        Task Rollback(Exception exception, bool commitOffsets = false);
    }
}
