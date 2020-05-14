// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.ErrorHandling
{
    internal class MessageHandlerResult
    {
        private MessageHandlerResult(bool isSuccessful, ErrorAction? action)
        {
            IsSuccessful = isSuccessful;
            Action = action;
        }

        public static MessageHandlerResult Success { get; } = new MessageHandlerResult(true, null);

        public bool IsSuccessful { get; }

        public ErrorAction? Action { get; }

        public static MessageHandlerResult Error(ErrorAction action) => new MessageHandlerResult(false, action);
    }
}
