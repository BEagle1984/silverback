// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Outbound.TransactionalOutbox
{
    /// <summary>
    ///     Manages the consumer transaction calling <c>Commit</c> or <c>Rollback</c> on the
    ///     <see cref="IOutboxWriter" /> when the database transaction is being completed.
    /// </summary>
    /// <remarks>
    ///     This isn't even necessary if using EF Core and the <see cref="DbOutboxWriter" />, since it is
    ///     already implicitly taking part in the save changes transaction.
    /// </remarks>
    public class OutboxTransactionManager
    {
        private readonly IOutboxWriter _queueWriter;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OutboxTransactionManager" /> class.
        /// </summary>
        /// <param name="queueWriter">
        ///     The <see cref="OutboxTransactionManager" /> to manage.
        /// </param>
        public OutboxTransactionManager(IOutboxWriter queueWriter)
        {
            _queueWriter = queueWriter;
        }

        [Subscribe]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "IDE0051", Justification = Justifications.CalledBySilverback)]
        private async Task OnTransactionCompletedAsync(TransactionCompletedEvent message) =>
            await _queueWriter.CommitAsync().ConfigureAwait(false);

        [Subscribe]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "IDE0051", Justification = Justifications.CalledBySilverback)]
        private async Task OnTransactionAbortedAsync(TransactionAbortedEvent message) =>
            await _queueWriter.RollbackAsync().ConfigureAwait(false);
    }
}
