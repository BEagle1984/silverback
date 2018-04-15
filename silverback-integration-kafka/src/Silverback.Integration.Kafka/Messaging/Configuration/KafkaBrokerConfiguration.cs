using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Configuration
{    
    /// <summary>
    ///  Kafka broker configuration extension
    /// </summary>
    public static class KafkaBrokerConfiguration
    {
        private static readonly Dictionary<string, object> __connectionSettings = new Dictionary<string, object>();

        /// <summary>
        /// Sets the connection parameter
        /// </summary>
        /// <param name="broker"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static KafkaBroker SetConnection(this KafkaBroker broker, Dictionary<string, object> configuration)
        {
            __connectionSettings.AddRange(configuration);
            return broker;
        }

        /// <summary>
        /// Add the producer to the broker
        /// </summary>
        /// <param name="broker"></param>
        /// <param name="config">The producer configuration</param>
        public static KafkaBroker WithPublicationStrategy(this KafkaBroker broker, Dictionary<string, object> config)
        {            
            broker.ProducerConfiguration = config;
            broker.ProducerConfiguration.AddRange(__connectionSettings);
            return broker;
        }
        /// <summary>
        /// Add the producer with name to the broker.
        /// </summary>
        /// <param name="broker">The Kafka broker.</param>
        /// <param name="topic">The producer configuration</param>
        /// <param name="handlerFunc">Func that return a delivery report handler</param>
        public static KafkaBroker SetDeliveryHandlerFor(this KafkaBroker broker, string topic,
            Func<IDeliveryReportHandler> handlerFunc)
        {
            broker.Publishers[topic] = handlerFunc;
            return broker;
        }

        private static void AddRange<T>(this ICollection<T> target, IEnumerable<T> source)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            foreach (var element in source)
                target.Add(element);
        }
    }
}
