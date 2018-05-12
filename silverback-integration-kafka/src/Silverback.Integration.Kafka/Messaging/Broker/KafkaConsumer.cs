using System;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{    
    /// <inheritdoc />
    public class KafkaConsumer : Consumer
    {
        private bool Retrieves;
        private readonly KafkaEndpoint _endpoint;
        private Consumer<byte[], byte[]> _consumer;

        /// <summary>
        /// Initializes a new instance of the <see cref="KafkaConsumer"/> class.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="consumer">The consumer.</param>
        public KafkaConsumer(KafkaBroker broker, KafkaEndpoint endpoint) :
            base(broker,endpoint)
        {
            _endpoint = endpoint;
        }

        
        internal void Connect()
        {
            _consumer = new Consumer<byte[], byte[]>(_endpoint.Configuration,
                new ByteArrayDeserializer(),
                new ByteArrayDeserializer());

            _consumer.OnPartitionsAssigned += (_, partitions) =>
            {
                // TODO: Log.Trace($"Assigned partitions: [{string.Join(", ", partitions)}], member id: {consumer.MemberId}");
                _consumer.Assign(partitions);
            };

            _consumer.OnPartitionsRevoked += (_, partitions) =>
            {
                // TODO: Log.Trace($"Revoked partitions: [{string.Join(", ", partitions)}]");
                _consumer.Unassign();
            };

            _consumer.Subscribe(_endpoint.Name);
            Retrieves = true;
            while (Retrieves)
            {
                if (!_consumer.Consume(out var msg, TimeSpan.FromMilliseconds(100)))
                {
                    continue;
                }
                HandleMessage(msg.Value);
            }
        }

        internal void Disconnect()
        {
            Retrieves = false;
            _consumer.Unassign();
            _consumer.Unsubscribe();
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Disconnect();
                _consumer.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
