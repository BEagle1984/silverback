using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    /// An error policy is used to handle errors that may occur while processing the incoming messages.
    /// </summary>
    public interface IErrorPolicy
    {
        bool CanHandle(IMessage failedMessage, int retryCount, Exception exception);

        ErrorAction HandleError(IMessage failedMessage, int retryCount, Exception exception);
    }
}