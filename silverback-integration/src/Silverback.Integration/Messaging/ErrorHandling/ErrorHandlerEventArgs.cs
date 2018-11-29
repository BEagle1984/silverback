using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.ErrorHandling
{
    public class ErrorHandlerEventArgs : EventArgs
    {
        public ErrorHandlerEventArgs(Exception exception, IMessage message, int retryCount)
        {
            Exception = exception;
            Message = message;
            RetryCount = retryCount;
            Action = ErrorAction.StopConsuming;
        }

        public Exception Exception { get; }
        public IMessage Message { get; }
        public int RetryCount { get; }
        public ErrorAction Action { get; set; }
    }
}