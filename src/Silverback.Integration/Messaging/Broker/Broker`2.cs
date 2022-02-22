// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

/// <summary>
///     The base class for all <see cref="IBroker" /> implementations.
/// </summary>
/// <typeparam name="TProducerConfiguration">
///     The type of the <see cref="ProducerConfiguration" /> that is used by this broker implementation.
/// </typeparam>
/// <typeparam name="TConsumerConfiguration">
///     The type of the <see cref="ConsumerConfiguration" /> that is used by this broker implementation.
/// </typeparam>
[SuppressMessage("Naming", "CA1724:Type names should not match namespaces", Justification = "Is not going to cause much confusion, not worth breaking compatibility.")]
public abstract class Broker<TProducerConfiguration, TConsumerConfiguration> : IBroker, IDisposable
    where TProducerConfiguration : ProducerConfiguration
    where TConsumerConfiguration : ConsumerConfiguration
{
    private const int MaxConnectParallelism = 2;

    private const int MaxDisconnectParallelism = 4;

    private readonly EndpointsConfiguratorsInvoker _endpointsConfiguratorsInvoker;

    private readonly ISilverbackLogger _logger;

    private readonly IServiceProvider _serviceProvider;

    private readonly List<IConsumer> _consumers = new();

    private readonly List<IProducer> _producers = new();

    private readonly Dictionary<ProducerConfiguration, IProducer> _configurationToProducerDictionary = new();

    private bool _disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Broker{TProducerConfiguration,TConsumerConfiguration}" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
    /// </param>
    protected Broker(IServiceProvider serviceProvider)
    {
        _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        _endpointsConfiguratorsInvoker = _serviceProvider.GetRequiredService<EndpointsConfiguratorsInvoker>();
        _logger = _serviceProvider.GetRequiredService<ISilverbackLogger<Broker<ProducerConfiguration, TConsumerConfiguration>>>();

        ProducerConfigurationType = typeof(TProducerConfiguration);
        ConsumerConfigurationType = typeof(TConsumerConfiguration);
    }

    /// <inheritdoc cref="IBroker.ProducerConfigurationType" />
    public Type ProducerConfigurationType { get; }

    /// <inheritdoc cref="IBroker.ConsumerConfigurationType" />
    public Type ConsumerConfigurationType { get; }

    /// <inheritdoc cref="IBroker.Producers" />
    public IReadOnlyList<IProducer> Producers => _producers;

    /// <inheritdoc cref="IBroker.Consumers" />
    public IReadOnlyList<IConsumer> Consumers
    {
        get
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);

            return _consumers;
        }
    }

    /// <inheritdoc cref="IBroker.IsConnected" />
    public bool IsConnected { get; private set; }

    private IEnumerable<IBrokerConnectedObject> ConnectedObjects => _producers.Cast<IBrokerConnectedObject>().Concat(_consumers);

    /// <inheritdoc cref="IBroker.GetProducerAsync(ProducerConfiguration)" />
    public virtual async Task<IProducer> GetProducerAsync(ProducerConfiguration configuration)
    {
        Check.NotNull(configuration, nameof(configuration));

        if (_disposed)
            throw new ObjectDisposedException(GetType().FullName);

        IProducer producer = GetOrInstantiateProducer(configuration);

        if (!producer.IsConnected && IsConnected)
            await producer.ConnectAsync().ConfigureAwait(false);

        return producer;
    }

    /// <inheritdoc cref="IBroker.GetProducer(ProducerConfiguration)" />
    public IProducer GetProducer(ProducerConfiguration configuration) =>
        AsyncHelper.RunSynchronously(() => GetProducerAsync(configuration));

    /// <inheritdoc cref="IBroker.GetProducer(string)" />
    public IProducer GetProducer(string endpointName)
    {
        Check.NotNullOrEmpty(endpointName, nameof(endpointName));

        if (_disposed)
            throw new ObjectDisposedException(GetType().FullName);

        return _producers.First(producer => producer.Configuration.RawName == endpointName || producer.Configuration.FriendlyName == endpointName);
    }

    /// <inheritdoc cref="IBroker.AddConsumer" />
    public virtual IConsumer AddConsumer(ConsumerConfiguration configuration)
    {
        Check.NotNull(configuration, nameof(configuration));

        if (_disposed)
            throw new ObjectDisposedException(GetType().FullName);

        _logger.LogCreatingNewConsumer(configuration);

        IConsumer consumer = InstantiateConsumer(
            (TConsumerConfiguration)configuration,
            _serviceProvider.GetRequiredService<IBrokerBehaviorsProvider<IConsumerBehavior>>(),
            _serviceProvider);

        lock (_consumers)
        {
            _consumers.Add(consumer);
        }

        if (IsConnected)
            consumer.ConnectAsync().FireAndForget();

        return consumer;
    }

    /// <inheritdoc cref="IBroker.ConnectAsync" />
    public async Task ConnectAsync()
    {
        if (IsConnected)
            return;

        if (_disposed)
            throw new ObjectDisposedException(GetType().FullName);

        await _endpointsConfiguratorsInvoker.InvokeAsync().ConfigureAwait(false);

        _logger.LogBrokerConnecting(this);

        await ConnectAsync(ConnectedObjects).ConfigureAwait(false);
        IsConnected = true;

        _logger.LogBrokerConnected(this);
    }

    /// <inheritdoc cref="IBroker.DisconnectAsync" />
    public async Task DisconnectAsync()
    {
        if (!IsConnected)
            return;

        if (_disposed)
            throw new ObjectDisposedException(GetType().FullName);

        _logger.LogBrokerDisconnecting(this);

        await DisconnectAsync(ConnectedObjects).ConfigureAwait(false);
        IsConnected = false;

        _logger.LogBrokerDisconnected(this);
    }

    /// <inheritdoc cref="IDisposable.Dispose" />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Returns a new instance of <see cref="IProducer" /> to publish to the specified endpoint. The
    ///     returned instance will be cached and reused for the same endpoint.
    /// </summary>
    /// <param name="configuration">
    ///     The <typeparamref name="TProducerConfiguration" />.
    /// </param>
    /// <param name="behaviorsProvider">
    ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> instance to be used to resolve the needed types or to be
    ///     forwarded to the consumer.
    /// </param>
    /// <returns>
    ///     The instantiated <see cref="IProducer" />.
    /// </returns>
    protected abstract IProducer InstantiateProducer(
        TProducerConfiguration configuration,
        IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
        IServiceProvider serviceProvider);

    /// <summary>
    ///     Returns a new instance of <see cref="IConsumer" /> to subscribe to the specified endpoint.
    /// </summary>
    /// <param name="configuration">
    ///     The <typeparamref name="TConsumerConfiguration" />.
    /// </param>
    /// <param name="behaviorsProvider">
    ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> instance to be used to resolve the needed types or to be
    ///     forwarded to the consumer.
    /// </param>
    /// <returns>
    ///     The instantiated <see cref="IConsumer" />.
    /// </returns>
    protected abstract IConsumer InstantiateConsumer(
        TConsumerConfiguration configuration,
        IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
        IServiceProvider serviceProvider);

    /// <summary>
    ///     Connects all the consumers and starts consuming.
    /// </summary>
    /// <param name="connectedObjects">
    ///     The producers and consumers to be connected.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    protected virtual Task ConnectAsync(IEnumerable<IBrokerConnectedObject> connectedObjects) =>
        connectedObjects.ParallelForEachAsync(connectedObject => connectedObject.ConnectAsync(), MaxConnectParallelism);

    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged  resources.
    /// </summary>
    /// <param name="disposing">
    ///     A value indicating whether the method has been called by the <c>Dispose</c> method and not from the finalizer.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing || _disposed)
            return;

        _disposed = true;

        AsyncHelper.RunSynchronously(DisconnectAsync);

        ConnectedObjects.OfType<IDisposable>().ForEach(disposable => disposable.Dispose());
    }

    /// <summary>
    ///     Disconnects all the consumers and stops consuming.
    /// </summary>
    /// <param name="connectedObjects">
    ///     The producers and consumers to be disconnected.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    protected virtual Task DisconnectAsync(IEnumerable<IBrokerConnectedObject> connectedObjects) =>
        connectedObjects.ParallelForEachAsync(
            connectedObject => connectedObject.DisconnectAsync(),
            MaxDisconnectParallelism);

    private IProducer GetOrInstantiateProducer(ProducerConfiguration configuration)
    {
        if (_configurationToProducerDictionary.TryGetValue(configuration, out IProducer? producer))
            return producer;

        lock (_producers)
        {
            if (_configurationToProducerDictionary.TryGetValue(configuration, out producer))
                return producer;

            _logger.LogCreatingNewProducer(configuration);

            producer = InstantiateProducer(
                (TProducerConfiguration)configuration,
                _serviceProvider.GetRequiredService<IBrokerBehaviorsProvider<IProducerBehavior>>(),
                _serviceProvider);

            _producers.Add(producer);
            _configurationToProducerDictionary.Add(configuration, producer);

            return producer;
        }
    }
}
