// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.ErrorHandling
{
    // TODO: Test
    public static class InboundMessageExtensions
    {
        public static void TryDeserializeAndProcess(this IInboundMessage message, IErrorPolicy errorPolicy, Action<IInboundMessage> messageHandler)
        {
            var attempt = message.FailedAttempts + 1;

            while (true)
            {
                var result = HandleMessage(message, messageHandler, errorPolicy, attempt);

                if (result.IsSuccessful || result.Action == ErrorAction.Skip)
                    return;

                attempt++;
            }
        }

        private static MessageHandlerResult HandleMessage(IInboundMessage message, Action<IInboundMessage> messageHandler, IErrorPolicy errorPolicy, int attempt)
        {
            try
            {
                message = DeserializeIfNeeded(message);

                messageHandler(message);

                return MessageHandlerResult.Success;
            }
            catch (Exception ex)
            {
                if (errorPolicy == null)
                    throw;

                UpdateFailedAttemptsHeader(message, attempt);

                if (!errorPolicy.CanHandle(message, ex))
                    throw;

                var action = errorPolicy.HandleError(message, ex);

                if (action == ErrorAction.StopConsuming)
                    throw;

                return MessageHandlerResult.Error(action);
            }
        }

        private static void UpdateFailedAttemptsHeader(IInboundMessage message, int attempt)
        {
            if (message is IInboundBatch batch)
                batch.Messages.ForEach(m => UpdateFailedAttemptsHeader(m, attempt));
            else
                message.Headers.Replace(MessageHeader.FailedAttemptsHeaderName, attempt.ToString());
        }

        private static IInboundMessage DeserializeIfNeeded(IInboundMessage message)
        {
            if (message is IInboundBatch batch)
                return new InboundBatch(
                        batch.Id,
                        batch.Messages.Select(DeserializeIfNeeded),
                        batch.Endpoint);

            if (message.Message is byte[])
                return InboundMessageHelper.CreateNewInboundMessage(
                    Deserialize(message),
                    message);
            
            return message;
        }

        private static object Deserialize(IInboundMessage message) =>
            message.Endpoint.Serializer.Deserialize((byte[])message.Message);
    }
}
