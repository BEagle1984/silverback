// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using Silverback.Messaging.Broker.Kafka.Mocks;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     The builder for the <see cref="MockedConfluentProducer" />.
/// </summary>
public class MockedConfluentProducerBuilder : IConfluentProducerBuilder
{
    private readonly IInMemoryTopicCollection _topics;

    private ProducerConfig? _config;

    private Action<IProducer<byte[]?, byte[]?>, string>? _statisticsHandler;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MockedConfluentProducerBuilder" /> class.
    /// </summary>
    /// <param name="topics">
    ///     The <see cref="IInMemoryTopicCollection" />.
    /// </param>
    public MockedConfluentProducerBuilder(IInMemoryTopicCollection topics)
    {
        _topics = topics;
    }

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
        // Not yet implemented / not needed
        return this;
    }

    /// <inheritdoc cref="IConfluentProducerBuilder.Build" />
    public IProducer<byte[]?, byte[]?> Build()
    {
        if (_config == null)
            throw new InvalidOperationException("SetConfig must be called to provide the producer configuration.");

        MockedConfluentProducer producer = new(_config, _topics);

        producer.StatisticsHandler = _statisticsHandler;

        return producer;
    }
}
