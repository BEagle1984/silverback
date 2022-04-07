// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka
{
    /// <summary>
    ///     The builder for the <see cref="MockedConfluentProducer" />.
    /// </summary>
    public class MockedConfluentProducerBuilder : IConfluentProducerBuilder
    {
        private readonly InMemoryTopicCollection _topics;

        private readonly InMemoryTransactionManager _transactionManager;

        private ProducerConfig? _config;

        private Action<IProducer<byte[]?, byte[]?>, string>? _statisticsHandler;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MockedConfluentProducerBuilder" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
        /// </param>
        public MockedConfluentProducerBuilder(IServiceProvider serviceProvider)
        {
            Check.NotNull(serviceProvider, nameof(serviceProvider));
            _topics = serviceProvider.GetRequiredService<InMemoryTopicCollection>();
            _transactionManager = serviceProvider.GetRequiredService<InMemoryTransactionManager>();
        }

        /// <inheritdoc cref="IConfluentProducerBuilder.SetConfig" />
        public IConfluentProducerBuilder SetConfig(ProducerConfig config)
        {
            _config = config;
            return this;
        }

        /// <inheritdoc cref="IConfluentProducerBuilder.SetStatisticsHandler" />
        public IConfluentProducerBuilder SetStatisticsHandler(Action<IProducer<byte[]?, byte[]?>, string> statisticsHandler)
        {
            _statisticsHandler = statisticsHandler;
            return this;
        }

        /// <inheritdoc cref="IConfluentProducerBuilder.SetLogHandler" />
        public IConfluentProducerBuilder SetLogHandler(Action<IProducer<byte[]?, byte[]?>, LogMessage> logHandler)
        {
            // Not yet implemented / not needed
            return this;
        }

        /// <inheritdoc cref="IConfluentProducerBuilder.Build" />
        public IProducer<byte[]?, byte[]?> Build()
        {
            if (_config == null)
                throw new InvalidOperationException("SetConfig must be called to provide the producer configuration.");

            var producer = new MockedConfluentProducer(_config, _topics, _transactionManager);

            producer.StatisticsHandler = _statisticsHandler;

            return producer;
        }
    }
}
