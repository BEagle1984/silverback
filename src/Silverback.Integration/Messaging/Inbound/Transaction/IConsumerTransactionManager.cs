// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Inbound.Transaction
{
    public interface IConsumerTransactionManager
    {
        void AddOffset(IOffset offset);

        void AddOffsets(IEnumerable<IOffset> offsets);

        /// <summary>
        ///     Adds the specified service to the transaction participants to be called upon commit or rollback.
        /// </summary>
        /// <param name="transactionalService">
        ///     The service to be enlisted.
        /// </param>
        void Enlist(ITransactional transactionalService);

        Task Commit();

        Task Rollback(bool commitOffsets = false);
    }
}
