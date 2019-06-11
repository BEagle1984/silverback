// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.ErrorHandling
{
    // TODO: Test
    public static class InboundMessageExtensions
    {
        public static void TryDeserializeAndProcess(this IInboundMessage message, IErrorPolicy errorPolicy, Action<IInboundMessage> messageHandler)
        {
            while (true)
            {
                var result = HandleMessage(message, messageHandler, errorPolicy);

                if (result.IsSuccessful || result.Action == ErrorAction.Skip)
                    return;
            }
        }

        private static MessageHandlerResult HandleMessage(IInboundMessage message, Action<IInboundMessage> messageHandler, IErrorPolicy errorPolicy)
        {
            try
            {
                message = message.Message is byte[]
                    ? InboundMessageHelper.CreateNewInboundMessage(Deserialize(message), message)
                    : message;

                messageHandler(message);

                return MessageHandlerResult.Success;
            }
            catch (Exception ex)
            {
                if (errorPolicy == null)
                    throw;

                message.Headers.Replace(MessageHeader.FailedAttemptsHeaderName, (message.FailedAttempts + 1).ToString());

                if (!errorPolicy.CanHandle(message, ex))
                    throw;

                var action = errorPolicy.HandleError(message, ex);

                if (action == ErrorAction.StopConsuming)
                    throw;

                return MessageHandlerResult.Error(action);
            }
        }

        private static object Deserialize(IInboundMessage message) =>
            message.Endpoint.Serializer.Deserialize((byte[]) message.Message);
    }
}
