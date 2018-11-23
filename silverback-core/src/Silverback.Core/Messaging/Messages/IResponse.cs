using System;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    /// Reprensent the response received to an <see cref="IRequest"/>.
    /// </summary>
    public interface IResponse : IMessage
    {
        Guid RequestId { get; set; }
    }
}