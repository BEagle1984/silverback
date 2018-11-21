using System;
using System.Collections.Generic;
using System.Text;

namespace Silverback.Messaging.Messages
{
    // TODO: Review and test
    public static class MessageTraceHelper
    {
        public static string GetTraceString(this IMessage message, IEndpoint endpoint)
        {
            if (message is IIntegrationMessage integrationMassage)
                return $"{integrationMassage.Id} (endpoint={endpoint.Name}, type={message.GetType().Name})";

            return $"? (endpoint={endpoint.Name})";
        }

        public static string GetTraceString(this IMessage message)
        {
            if (message is IIntegrationMessage integrationMassage)
                return $"{integrationMassage.Id} (type={message.GetType().Name})";

            return $"? (type={message.GetType().Name})"; //TODO: Can do better than this?
        }
    }
}
