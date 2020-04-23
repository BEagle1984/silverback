// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    ///     Manages the consumer transaction calling <c> Commit </c> or <c> Rollback </c> on the
    ///     <see cref="IOutboundQueueWriter" /> when the database transaction is being completed.
    /// </summary>
    /// <remarks>
    ///     This isn't even necessary if using EF Core and the <see cref="DbOutboundQueueWriter" />, since it
    ///     is already implicitly taking part in the save changes transaction.
    /// </remarks>
    public class DeferredOutboundConnectorTransactionManager : ISubscriber
    {
        private readonly IOutboundQueueWriter _queueWriter;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DeferredOutboundConnectorTransactionManager" /> class.
        /// </summary>
        /// <param name="queueWriter">
        ///     The <see cref="IOutboundQueueWriter" /> to manage.
        /// </param>
        public DeferredOutboundConnectorTransactionManager(IOutboundQueueWriter queueWriter)
        {
            _queueWriter = queueWriter;
        }

        [Subscribe]
        [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = Justifications.Subscriber)]
        internal async Task OnTransactionCompleted(TransactionCompletedEvent message) => await _queueWriter.Commit();

        [Subscribe]
        [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = Justifications.Subscriber)]
        internal async Task OnTransactionAborted(TransactionAbortedEvent message) => await _queueWriter.Rollback();
    }
}
