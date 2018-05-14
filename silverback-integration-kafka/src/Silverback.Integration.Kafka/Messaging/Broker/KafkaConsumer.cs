using System;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{    
    /// <inheritdoc />
    public class KafkaConsumer : Consumer
    {
        private bool _disconnected = false;
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

        /// <inheritdoc />
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
            
            while (!_disconnected)
            {
                if (!_consumer.Consume(out var msg, TimeSpan.FromMilliseconds(100)))
                    continue;

                HandleMessage(msg.Value);
                if (!IsAutocommitEnabled)
                {
                    var committedOffsets = _consumer.CommitAsync(msg).Result;
                    // TODO: Log.Trace($"Committed offset: {committedOffsets}");
                }
            }
        }

        /// <inheritdoc />
        internal void Disconnect()
        {
            Disconnected = true;
            _consumer.Unassign();
            _consumer.Unsubscribe();
        }

        private bool IsAutocommitEnabled =>
            _endpoint.Configuration.ContainsKey("enable.auto.commit") ?
            (bool)_endpoint.Configuration["enable.auto.commit"] :
            false;

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
