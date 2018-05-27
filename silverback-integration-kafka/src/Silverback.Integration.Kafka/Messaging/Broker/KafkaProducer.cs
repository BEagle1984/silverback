using System;
using System.Threading.Tasks;
using Common.Logging;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc />
    /// <summary>
    /// A Apache Kafka bases <see cref="IProducer" />
    /// </summary>
    /// <seealso cref="Silverback.Messaging.Broker.Producer" />
    /// <seealso cref="Silverback.Messaging.Broker.IProducer" />
    public class KafkaProducer : Producer
    {
        private readonly KafkaEndpoint _endpoint;
        private readonly KafkaBroker _broker;
        private Producer<byte[], byte[]> _producer;
        private readonly ILog _log;

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="KafkaProducer"/> class.
        /// </summary>
        /// <param name="broker">The broker.</param>
        /// <param name="endpoint">The endpoint.</param>
        public KafkaProducer(IBroker broker, KafkaEndpoint endpoint) : base(broker, endpoint)
        {
            _endpoint = endpoint;
            _broker = (KafkaBroker)broker;
        }
            

        internal void Connect()
        {
            if (_producer != null) return;

            // TODO: (REVIEW) So we finally decided to create a producer per each endpoint?
            // Isn't this against the experts suggestions?
            // The problem is that the _endpoint contains the topic name as well, so the Broker class 
            // will not reuse it (since your IEquatable implementation)
            // We may need to:
            //      1) Implement another Producer cache here inside the KafkaProducer to reuse them according to a custom
            //         logic (custom comparer provided to the dictionary or whatever)
            //      2) Allow the Broker base class to be injected with a compararer to be used for producers / consumers 
            //         cache (so that we don't need to implement the equality logic in the endpoint itself.
            //      3) Use different endpoint classes for Producer and Consumer (e.g. KafkaProducerEndpoint and KafkaConsumerEndpoint)
            // The option 2 looks the best to me but we have to discuss and there may easily be some other options I didn't consider
            _producer = new Producer<byte[], byte[]>(
                _endpoint.Configuration,
                    new ByteArraySerializer(),
                    new ByteArraySerializer());
        }

        internal void Disconnect()
        {
            _producer?.Dispose();
            _producer = null;
        }

        /// <inheritdoc />
        protected override void Produce(IIntegrationMessage message, byte[] serializedMessage)
        {
            var deliveryReport = _producer.ProduceAsync(_endpoint.Name, KeyHelper.GetMessageKey(message), serializedMessage).Result;
            if(deliveryReport.Error.HasError) throw new Exception(deliveryReport.Error.Reason);
            //=> ProduceAsync(message, serializedMessage).RunSynchronously();
        }

        /// <inheritdoc />
        protected override async Task ProduceAsync(IIntegrationMessage message, byte[] serializedMessage)
        {
            var msg = await _producer.ProduceAsync(_endpoint.Name, KeyHelper.GetMessageKey(message), serializedMessage);
            if(msg.Error.HasError) _log.Fatal(msg.Error.Reason); 
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Disconnect();

            base.Dispose(disposing);
        }
    }
}
