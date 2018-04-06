using System;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    /// Reprensent the response received to a <see cref="IRequest"/>.
    /// </summary>
    /// <seealso cref="IMessage" />
    public interface IResponse : IMessage
    {
        /// <summary>
        /// Gets or sets the request identifier.
        /// </summary>
        Guid RequestId { get; set; }
    }
}