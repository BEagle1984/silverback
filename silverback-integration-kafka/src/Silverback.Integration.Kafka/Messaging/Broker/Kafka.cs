using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker
{
    public class Kafka : Broker
    {
        private readonly ConcurrentBag<(string topic,
            Dictionary<string, object> settings,
            Func<IDeliveryResultHandler> handlerFactory,
            IProducer producer)> _producers = new ConcurrentBag<(string, Dictionary<string, object>, Func<IDeliveryResultHandler>, IProducer)>();

        /// <inheritdoc />
        public override IConsumer GetConsumer(IEndpoint endpoint)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override IProducer GetProducer(IEndpoint endpoint)
        {
            var producerSettings = _producers.First(p => p.topic == endpoint.Name);
            if (producerSettings.producer != null)
                return producerSettings.producer;

            producerSettings.producer = new KafkaProducer(endpoint,
                producerSettings.settings,
                new DeliveryHandler(producerSettings.handlerFactory));

            return producerSettings.producer;
        }

        #region configurations
        /// <summary>
        /// Add the producer to the broker
        /// </summary>
        public Kafka AddProducer(string topicName, Dictionary<string, object> settings)
        {
            _producers.Add((topicName, settings, null, null));
            return this;
        }

        /// <summary>
        /// Add the producer to the broker
        /// </summary>
        public Kafka AddProducer(string topicName,
            Dictionary<string, object> settings,
            Func<IDeliveryResultHandler> handlerFactory)
        {
            _producers.Add((topicName, settings, handlerFactory, null));
            return this;
        }

        /// <inheritdoc />
        public override void ValidateConfiguration()
        {
            var errorMessages = new List<string>();

            //Check if exists more then one producer for same topic
            var pForTopic = _producers.GroupBy(info => info.topic)
                .Select(group => new
                {
                    Metric = @group.Key,
                    Count = @group.Count()
                });

            if (pForTopic.Any(p => p.Count > 1)) errorMessages.Add("More then one producer for same topic.");

            //TODO: Add other validator
            if (errorMessages.Count > 0)
                throw new InvalidOperationException(string.Join(", ", errorMessages));
        }

        #endregion
    }

}
