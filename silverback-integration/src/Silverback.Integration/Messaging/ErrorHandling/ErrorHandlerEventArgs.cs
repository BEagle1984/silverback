// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.ErrorHandling
{
    public class ErrorHandlerEventArgs : EventArgs
    {
        public ErrorHandlerEventArgs(Exception exception, FailedMessage message)
        {
            Exception = exception;
            FailedMessage = message;
            Action = ErrorAction.StopConsuming;
        }

        public Exception Exception { get; }
        public FailedMessage FailedMessage { get; }
        public ErrorAction Action { get; set; }
    }
}