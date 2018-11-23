using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    /// An error policy is used to handle errors that may occur while processing the incoming messages.
    /// </summary>
    public interface IErrorPolicy
    {
        void TryHandleMessage(IMessage message, Action<IMessage> messageHandler);
    }
}