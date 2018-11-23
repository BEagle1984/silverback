using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Silverback.Messaging.Broker
{
    public class KafkaConsumer : Consumer
    {
        private Confluent.Kafka.Consumer<byte[], byte[]> _consumer;
        private readonly ILogger<KafkaConsumer> _logger;

        /// <inheritdoc />
        public KafkaConsumer(IBroker broker, KafkaEndpoint endpoint, ILogger<KafkaConsumer> logger) :
            base(broker,endpoint, logger)
        {
            _logger = logger;
        }

        public new KafkaEndpoint Endpoint => (KafkaEndpoint) base.Endpoint;

        internal void Connect()
        {
            if (_consumer != null)
                return;

            _consumer = new Confluent.Kafka.Consumer<byte[], byte[]>(Endpoint.Configuration,
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
             
            Task.Run(StartConsuming);
        }

        internal void Disconnect()
        {
            _disconnected = true;
            _consumer?.Unassign();
            _consumer?.Unsubscribe();
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Disconnect();
                _consumer?.Dispose();
                _consumer = null;
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Starts the poll process to retrieve the messages. 
        /// </summary>
        /// <returns></returns>
        private async Task StartConsuming()
        {            
            while (Broker.IsConnected)
            {
                if (!_consumer.Consume(out var message, TimeSpan.FromMilliseconds(_endpoint.TimeoutPollBlock)))
                    continue;

                HandleMessage(message.Value);
                if (IsAutocommitEnabled) continue;
                if (message.Offset % _endpoint.CommitOffsetEach != 0) continue;
                var committedOffsets = await _consumer.CommitAsync(message);
                _log.Trace($"Committed offset: {committedOffsets}");
            }
        }
    }
}
