// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.ConfluentWrappers
{
    /// <summary>
    ///     Wraps the <see cref="Confluent.Kafka.ProducerBuilder{TKey,TValue}" />.
    /// </summary>
    [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
    public class ConfluentProducerBuilder : IConfluentProducerBuilder
    {
        private ProducerConfig? _config;

        private Action<IProducer<byte[]?, byte[]?>, string>? _statisticsHandler;

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

            var builder = new ProducerBuilder<byte[]?, byte[]?>(_config);

            if (_statisticsHandler != null)
                builder.SetStatisticsHandler(_statisticsHandler);

            return builder.Build();
        }
    }
}
