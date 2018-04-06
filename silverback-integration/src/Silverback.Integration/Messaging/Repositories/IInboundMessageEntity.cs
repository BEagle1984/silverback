using System;

namespace Silverback.Messaging.Repositories
{
    /// <summary>
    /// The interface for the entity to be stored in the inbox table.
    /// </summary>
    public interface IInboundMessageEntity
    {
        /// <summary>
        /// Gets or sets the message unique identifier.
        /// </summary>
        Guid MessageId { get; set; }

        /// <summary>
        /// Gets or sets the received timestamp.
        /// </summary>
        DateTime Received { get; set; }
    }
}