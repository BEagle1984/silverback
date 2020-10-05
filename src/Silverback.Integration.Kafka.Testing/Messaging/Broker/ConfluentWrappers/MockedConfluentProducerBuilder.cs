// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using Silverback.Messaging.Broker.Topics;

namespace Silverback.Messaging.Broker.ConfluentWrappers
{
    /// <summary>
    ///     The builder for the <see cref="MockedKafkaProducer" />.
    /// </summary>
    [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
    public class MockedConfluentProducerBuilder : IConfluentProducerBuilder
    {
        private readonly IInMemoryTopicCollection _topics;

        private ProducerConfig? _config;

        private Action<IProducer<byte[]?, byte[]?>, string>? _statisticsHandler;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MockedConfluentProducerBuilder" /> class.
        /// </summary>
        /// <param name="topics">
        ///     The <see cref="InMemoryTopicCollection" />.
        /// </param>
        public MockedConfluentProducerBuilder(IInMemoryTopicCollection topics)
        {
            _topics = topics;
        }

        /// <inheritdoc cref="IConfluentProducerBuilder.SetConfig" />
        public IConfluentProducerBuilder SetConfig(ProducerConfig config)
        {
            _config = config;
            return this;
        }

        /// <inheritdoc cref="IConfluentProducerBuilder.SetStatisticsHandler" />
        public IConfluentProducerBuilder SetStatisticsHandler(
            Action<IProducer<byte[]?, byte[]?>, string> statisticsHandler)
        {
            _statisticsHandler = statisticsHandler;
            return this;
        }

        /// <inheritdoc cref="IConfluentProducerBuilder.Build" />
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
