using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Repositories
{
    public interface IOutboundQueueWriter
    {
        void Enqueue(IIntegrationMessage message, IEndpoint endpoint);

        void Commit();

        void Rollback();
    }

    public interface IOutboundQueueReader
    {
        int Length { get; }

        IEnumerable<QueuedMessage> Dequeue(int count);

        /// <summary>
        /// Re-enqueue the message to retry.
        /// </summary>
        void Retry(QueuedMessage message);

        /// <summary>
        /// Acknowledges the specified message has been sent.
        /// </summary>
        void Acknowledge(QueuedMessage message);
    }
}