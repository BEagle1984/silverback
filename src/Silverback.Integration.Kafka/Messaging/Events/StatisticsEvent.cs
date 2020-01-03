using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Events
{
    public class StatisticsEvent : IMessage
    {
        public StatisticsEvent(string json)
        {
            Json = json;
        }

        public string Json { get; set; }
    }
}
