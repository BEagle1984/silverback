namespace Silverback.Messaging.Messages
{
    /// <summary>
    /// 
    /// </summary>
    public class KafkaStatisticsEvent : IMessage
    {
        public KafkaStatisticsEvent(string json)
        {
            Json = json;
        }

        public string Json { get; }
    }
}
