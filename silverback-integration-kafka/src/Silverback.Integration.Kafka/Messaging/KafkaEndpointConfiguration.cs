using System.Collections.Generic;

namespace Silverback.Messaging
{
    /// <summary>
    /// Extends the Kafka configuration dictionary.
    /// </summary>
    /// <seealso cref="System.Collections.Generic.Dictionary{System.String, System.Object}" />
    public class KafkaEndpointConfiguration : Dictionary<string, object>
    {
        public bool IsAutocommitEnabled
        {
            get => ContainsKey("enable.auto.commit") && (bool)this["enable.auto.commit"];
            set => this["enable.auto.commit"] = value;
        }
    }
}