// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <inheritdoc cref="IBrokerClientsInitializer" />
public abstract class BrokerClientsInitializer : IBrokerClientsInitializer
{
    private readonly ISilverbackLogger<IBrokerClientsInitializer> _logger;

    private readonly ProducerCollection _producers;

    private readonly ConsumerCollection _consumers;

    private readonly BrokerClientCollection _clients;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BrokerClientsInitializer" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the necessary services.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="ISilverbackLogger{TCategoryName}" />.
    /// </param>
    protected BrokerClientsInitializer(IServiceProvider serviceProvider, ISilverbackLogger<IBrokerClientsInitializer> logger)
    {
        // Ensure that the root service provider is used to resolve the needed service, to avoid premature disposal
        ServiceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider)).GetRequiredService<RootServiceProvider>().ServiceProvider;
        _logger = Check.NotNull(logger, nameof(logger));

        _producers = serviceProvider.GetRequiredService<ProducerCollection>();
        _consumers = serviceProvider.GetRequiredService<ConsumerCollection>();
        _clients = serviceProvider.GetRequiredService<BrokerClientCollection>();
    }

    /// <summary>
    ///     Gets the root <see cref="IServiceProvider" />.
    /// </summary>
    protected IServiceProvider ServiceProvider { get; }

    /// <inheritdoc cref="IBrokerClientsInitializer.Initialize" />
    public void Initialize() => InitializeCore();

    /// <inheritdoc cref="IBrokerClientsInitializer.Initialize" />
    protected abstract void InitializeCore();

    /// <summary>
    ///     Registers the <see cref="IBrokerClient" />.
    /// </summary>
    /// <param name="client">
    ///     The <see cref="IBrokerClient" /> to be added.
    /// </param>
    protected void AddClient(IBrokerClient client)
    {
        Check.NotNull(client, nameof(client));

        _clients.Add(client);
        _logger.LogBrokerClientCreated(client);
    }

    /// <summary>
    ///     Registers the <see cref="IProducer" />.
    /// </summary>
    /// <param name="producer">
    ///     The <see cref="IProducer" /> to be added.
    /// </param>
    /// <param name="routing">
    ///     A value indicating whether the producer endpoints must implicitly be added to the mapped outbound routes (according to the
    ///     message type).
    /// </param>
    protected void AddProducer(IProducer producer, bool routing = true)
    {
        Check.NotNull(producer, nameof(producer));

        _producers.Add(
            Check.NotNull(producer, nameof(producer)),
            routing && producer.EndpointConfiguration.MessageType != typeof(object));

        _logger.LogProducerCreated(producer);
    }

    /// <summary>
    ///     Registers the <see cref="IConsumer" />.
    /// </summary>
    /// <param name="consumer">
    ///     The <see cref="IConsumer" /> to be added.
    /// </param>
    protected void AddConsumer(IConsumer consumer)
    {
        Check.NotNull(consumer, nameof(consumer));

        _consumers.Add(consumer);

        _logger.LogConsumerCreated(consumer);
    }
}
