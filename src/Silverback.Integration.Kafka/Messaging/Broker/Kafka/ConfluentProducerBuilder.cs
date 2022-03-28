// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     Wraps the <see cref="Confluent.Kafka.ProducerBuilder{TKey,TValue}" />.
/// </summary>
public class ConfluentProducerBuilder : IConfluentProducerBuilder
{
    private ProducerConfig? _config;

    private Action<IProducer<byte[]?, byte[]?>, string>? _statisticsHandler;

    private Action<IProducer<byte[]?, byte[]?>, LogMessage>? _logHandler;

    /// <inheritdoc cref="IConfluentProducerBuilder.SetConfiguration" />
    public IConfluentProducerBuilder SetConfiguration(ProducerConfig config)
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
        _logHandler = logHandler;
        return this;
    }

    /// <inheritdoc cref="IConfluentProducerBuilder.Build" />
    public IProducer<byte[]?, byte[]?> Build()
    {
        if (_config == null)
            throw new InvalidOperationException("SetConfig must be called to provide the producer configuration.");

        ProducerBuilder<byte[]?, byte[]?> builder = new(_config);

        if (_statisticsHandler != null)
            builder.SetStatisticsHandler(_statisticsHandler);

        if (_logHandler != null)
            builder.SetLogHandler(_logHandler);

        return builder.Build();
    }
}
