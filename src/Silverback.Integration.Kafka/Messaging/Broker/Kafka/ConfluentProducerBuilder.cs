// Copyright (c) 2026 Sergio Aquilini
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

    private Action<IProducer<byte[]?, byte[]?>, Error>? _errorHandler;

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

    /// <inheritdoc cref="IConfluentProducerBuilder.SetErrorHandler" />
    public IConfluentProducerBuilder SetErrorHandler(Action<IProducer<byte[]?, byte[]?>, Error> errorHandler)
    {
        _errorHandler = errorHandler;
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

        if (_errorHandler != null)
            builder.SetErrorHandler(_errorHandler);

        return builder.Build();
    }
}
