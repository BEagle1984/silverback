using System;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    /// Reprensent a request message awaiting a response (see <see cref="IResponse"/>).
    /// </summary>
    /// <seealso cref="IMessage" />
    public interface IRequest : IMessage
    {
        /// <summary>
        /// Gets or sets the request identifier.
        /// </summary>
        Guid RequestId { get; set; }
    }
}