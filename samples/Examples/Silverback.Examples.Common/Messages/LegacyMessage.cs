using Silverback.Messaging.Messages;

namespace Silverback.Examples.Common.Messages
{
    public class LegacyMessage : IMessage
    {
        public string Content { get; set; }
    }
}