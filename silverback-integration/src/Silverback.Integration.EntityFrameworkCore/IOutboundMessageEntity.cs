using System;

namespace Silverback.Integration.EntityFrameworkCore
{
    /// <summary>
    /// The interface for the entity to be stored in the outbox table.
    /// </summary>
    public interface IOutboundMessageEntity
    {
        /// <summary>
        /// Gets or sets the message unique identifier.
        /// </summary>
        Guid MessageId { get; set; }

        /// <summary>
        /// Gets or sets the fully qualified name of the message type.
        /// </summary>
        string MessageType { get; set; }

        /// <summary>
        /// Gets or sets the JSON serialized message.
        /// </summary>
        string Message { get; set; }

        /// <summary>
        /// Gets or sets the fully qualified name of the endpoint.
        /// </summary>
        string EndpointType { get; set; }

        /// <summary>
        /// Gets or sets the JSON serialized endpoint information.
        /// </summary>
        string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the created timestamp.
        /// </summary>
        DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets the sent timestamp.
        /// </summary>
        DateTime? Sent { get; set; }
    }
}