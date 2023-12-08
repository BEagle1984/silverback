﻿// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka;

internal class ConfluentProducerWrapper : BrokerClient, IConfluentProducerWrapper
{
    private readonly IConfluentProducerBuilder _producerBuilder;

    private readonly KafkaProducerConfiguration _configuration;

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Life cycle externally handled")]
    private IProducer<byte[]?, byte[]?>? _confluentProducer;

    public ConfluentProducerWrapper(
        string name,
        IConfluentProducerBuilder producerBuilder,
        KafkaProducerConfiguration configuration,
        IBrokerClientCallbacksInvoker brokerClientCallbacksInvoker,
        ISilverbackLogger<ConfluentProducerWrapper> logger)
        : base(name, logger)
    {
        _configuration = Check.NotNull(configuration, nameof(configuration));
        Check.NotNull(brokerClientCallbacksInvoker, nameof(brokerClientCallbacksInvoker));

        _producerBuilder = Check.NotNull(producerBuilder, nameof(producerBuilder))
            .SetConfiguration(configuration.GetConfluentClientConfig())
            .SetEventsHandlers(this, brokerClientCallbacksInvoker, Check.NotNull(logger, nameof(logger)));
    }

    public void Produce(TopicPartition topicPartition, Message<byte[]?, byte[]?> message, Action<DeliveryReport<byte[]?, byte[]?>> deliveryHandler)
    {
        if (Status != ClientStatus.Initialized)
            throw new InvalidOperationException("The producer is not connected.");

        if (_confluentProducer == null)
            throw new InvalidOperationException("The underlying producer is not initialized.");

        try
        {
            _confluentProducer.Produce(topicPartition, message, deliveryHandler);
        }
        catch (KafkaException)
        {
            if (_configuration.DisposeOnException)
                DisposeConfluentProducer();

            throw;
        }
    }

    public Task<DeliveryResult<byte[]?, byte[]?>> ProduceAsync(TopicPartition topicPartition, Message<byte[]?, byte[]?> message)
    {
        if (Status != ClientStatus.Initialized)
            throw new InvalidOperationException("The producer is not connected.");

        if (_confluentProducer == null)
            throw new InvalidOperationException("The underlying producer is not initialized.");

        try
        {
            return _confluentProducer.ProduceAsync(topicPartition, message);
        }
        catch (KafkaException)
        {
            if (_configuration.DisposeOnException)
                DisposeConfluentProducer();

            throw;
        }
    }

    protected override ValueTask ConnectCoreAsync()
    {
        _confluentProducer = _producerBuilder.Build();
        return default;
    }

    protected override ValueTask DisconnectCoreAsync()
    {
        _confluentProducer?.Flush();
        DisposeConfluentProducer();

        return default;
    }

    private void DisposeConfluentProducer()
    {
        _confluentProducer?.Dispose();
        _confluentProducer = null;
    }
}