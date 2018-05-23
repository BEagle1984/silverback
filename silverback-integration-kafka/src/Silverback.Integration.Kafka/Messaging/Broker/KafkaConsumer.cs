using Common.Logging;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using System;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc />
    public class KafkaConsumer : Consumer
    {
        private bool _disconnected;
        private readonly KafkaEndpoint _endpoint;
        private Consumer<byte[], byte[]> _consumer;
        private readonly ILog _log;

        /// <inheritdoc />
        public KafkaConsumer(IBroker broker, KafkaEndpoint endpoint) :
            base(broker,endpoint)
        {
            _endpoint = endpoint;
            _log = LogManager.GetLogger<KafkaConsumer>();
        }

        /// <summary>
        /// Gets a value indicating whether this instance is autocommit enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is autocommit enabled; otherwise, <c>false</c>.
        /// </value>
        private bool IsAutocommitEnabled =>
            _endpoint.Configuration.ContainsKey("enable.auto.commit") && (bool)_endpoint.Configuration["enable.auto.commit"];

        internal void Connect()
        {
            if (_consumer != null) return;

            _consumer = new Consumer<byte[], byte[]>(_endpoint.Configuration,
                new ByteArrayDeserializer(),
                new ByteArrayDeserializer());

            _consumer.OnPartitionsAssigned += (_, partitions) =>
            {
                _log.Trace($"Assigned partitions: [{string.Join(", ", partitions)}], member id: {_consumer.MemberId}");
                _consumer.Assign(partitions);
            };

            _consumer.OnPartitionsRevoked += (_, partitions) =>
            {
                _log.Trace($"Revoked partitions: [{string.Join(", ", partitions)}]");
                _consumer.Unassign();
            };

            _consumer.Subscribe(_endpoint.Name);

            // TODO: (REVIEW) This must be executed in another thread. 
            // The Connect() method is of course expected to exit so that the framework can go ahead
            // connecting and starting up the rest of the application.
            // Plus, it would be great if we could await the CommitAsync.
            // Something like: Task.Run(async () => { ... await ...; ... });
            while (!_disconnected)
            {
                if (!_consumer.Consume(out var msg, TimeSpan.FromMilliseconds(_endpoint.TimeoutPollBlock)))
                    continue;

                HandleMessage(msg.Value);
                if (IsAutocommitEnabled) continue;
                if (msg.Offset % _endpoint.CommitOffsetEach != 0) continue;
                var committedOffsets = _consumer.CommitAsync(msg).Result;
                _log.Trace($"Committed offset: {committedOffsets}");
            }
        }

        internal void Disconnect()
        {
            _disconnected = true;
            _consumer.Unassign();
            _consumer.Unsubscribe();
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Disconnect();
                // TODO: (REVIEW) Check for null (for example _consumer?.Dispose()) to avoid errors if Dispose is called twice (do the same in Disconnect() method)
                _consumer.Dispose();
                _consumer = null;
            }
            base.Dispose(disposing);
        }
    }
}
