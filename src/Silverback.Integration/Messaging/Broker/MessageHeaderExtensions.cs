using Silverback.Messaging.Messages;
using System.Collections.Generic;
using System.Linq;

namespace Silverback.Messaging.Broker
{
    public static class MessageHeaderExtensions
    {
        // TODO: Test
        public static string GetFromHeaders(this IEnumerable<MessageHeader> headers, string key)
        {
            var header = headers?.FirstOrDefault(h => h.Key == key);
            string value = null;
            if (header != null && !string.IsNullOrWhiteSpace(header.Value))
            {
                value = header.Value;
            }

            return value;
        }
    }
}
