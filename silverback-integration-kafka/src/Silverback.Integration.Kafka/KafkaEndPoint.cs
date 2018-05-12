using Silverback.Messaging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Silverback
{
    public sealed class KafkaEndpoint : IEndpoint
    {
        public KafkaEndpoint(string name, Dictionary<string, object> configs, string brokerName = null)
        {
            Name = name;
            Configuration = configs;
            BrokerName = brokerName;
        }

        public static KafkaEndpoint Create(string name, Dictionary<string, object> configs)
        {
            return new KafkaEndpoint(name, configs);
        }

        public string Name { get; set; }

        public string BrokerName { get; set; }

        public Dictionary<string, object> Configuration { get; set; }
    }
}
