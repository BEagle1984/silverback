// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     The builder for the <see cref="MockedConfluentProducer" />.
/// </summary>
public class MockedConfluentProducerBuilder : IConfluentProducerBuilder
{
    private readonly IInMemoryTopicCollection _topics;

    private readonly IInMemoryTransactionManager _transactionManager;

    private ProducerConfig? _config;

    private Action<IProducer<byte[]?, byte[]?>, string>? _statisticsHandler;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MockedConfluentProducerBuilder" /> class.
    /// </summary>
    /// <param name="topics">
    ///     The <see cref="IInMemoryTopicCollection" />.
    /// </param>
    /// <param name="transactionManager">
    ///     The <see cref="IInMemoryTransactionManager" />.
    /// </param>
    public MockedConfluentProducerBuilder(IInMemoryTopicCollection topics, IInMemoryTransactionManager transactionManager)
    {
        _topics = Check.NotNull(topics, nameof(topics));
        _transactionManager = Check.NotNull(transactionManager, nameof(transactionManager));
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
    // Not yet implemented / not needed
    public IConfluentProducerBuilder SetLogHandler(Action<IProducer<byte[]?, byte[]?>, LogMessage> logHandler) => this;

    /// <inheritdoc cref="IConfluentProducerBuilder.Build" />
    public IProducer<byte[]?, byte[]?> Build()
    {
        if (_config == null)
            throw new InvalidOperationException("SetConfig must be called to provide the producer configuration.");

        return new MockedConfluentProducer(_config, _topics, _transactionManager)
        {
            StatisticsHandler = _statisticsHandler
        };
    }
}
