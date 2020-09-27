// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using Silverback.Messaging.Broker.Topics;

namespace Silverback.Messaging.Broker.ConfluentWrappers
{
    public class MockedConfluentProducerBuilder : IConfluentProducerBuilder
    {
        private readonly IInMemoryTopicCollection _topics;

        private ProducerConfig? _config;

        private Action<IProducer<byte[]?, byte[]?>, string>? _statisticsHandler;

        public MockedConfluentProducerBuilder(IInMemoryTopicCollection topics)
        {
            _topics = topics;
        }

        public IConfluentProducerBuilder SetConfig(ProducerConfig config)
        {
            _config = config;
            return this;
        }

        public IConfluentProducerBuilder SetStatisticsHandler(
            Action<IProducer<byte[]?, byte[]?>, string> statisticsHandler)
        {
            _statisticsHandler = statisticsHandler;
            return this;
        }

        public IProducer<byte[]?, byte[]?> Build()
        {
            if (_config == null)
                throw new InvalidOperationException("SetConfig must be called to provide the producer configuration.");

            var producer = new MockedKafkaProducer(_config, _topics);

            // TODO: Use event handlers
            // if (_statisticsHandler != null)
            //     builder.SetStatisticsHandler(_statisticsHandler);

            return producer;
        }
    }
}
