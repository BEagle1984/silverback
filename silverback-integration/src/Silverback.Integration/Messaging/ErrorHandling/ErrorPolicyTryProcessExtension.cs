// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.ErrorHandling
{
    // TODO: Test
    public static class ErrorPolicyTryProcessExtension
    {
        public static void TryProcess(this IErrorPolicy errorPolicy, IMessage message, Action<IMessage> messageHandler)
        {
            int retryCount = 0;

            while (true)
            {
                var result = HandleMessage(message, messageHandler, retryCount, errorPolicy);

                if (result.IsSuccessful || result.Action == ErrorAction.Skip)
                    return;

                retryCount++;
            }
        }

        private static MessageHandlerResult HandleMessage(IMessage message, Action<IMessage> messageHandler,
            int retryCount, IErrorPolicy errorPolicy)
        {
            try
            {
                if (retryCount > 0)
                    message = IncrementFailedAttempts(message, retryCount);

                messageHandler(message);

                return MessageHandlerResult.Success;
            }
            catch (Exception ex)
            {
                if (errorPolicy == null)
                    throw;

                message = IncrementFailedAttempts(message);

                if (!errorPolicy.CanHandle((FailedMessage)message, ex))
                    throw;

                var action = errorPolicy.HandleError((FailedMessage)message, ex);

                if (action == ErrorAction.StopConsuming)
                    throw;

                return MessageHandlerResult.Error(action);
            }
        }

        private static FailedMessage IncrementFailedAttempts(IMessage message, int increment = 1)
        {
            if (message is FailedMessage failedMessage)
            {
                failedMessage.FailedAttempts += increment;
                return failedMessage;
            }

            return new FailedMessage(message, increment);
        }
    }
}
