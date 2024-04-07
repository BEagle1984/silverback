// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Routing;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

/// <inheritdoc cref="IProducer" />
public abstract class Producer : IProducer, IDisposable
{
    private readonly IReadOnlyList<IProducerBehavior> _behaviors;

    private readonly IServiceProvider _serviceProvider;

    private readonly IProducerLogger<IProducer> _logger;

    private readonly IOutboundEnvelopeFactory _envelopeFactory;

    private bool _isDisposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Producer" /> class.
    /// </summary>
    /// <param name="name">
    ///     The producer name.
    /// </param>
    /// <param name="client">
    ///     The <see cref="IBrokerClient" />.
    /// </param>
    /// <param name="endpointConfiguration">
    ///     The <see cref="ProducerEndpointConfiguration{TEndpoint}" />.
    /// </param>
    /// <param name="behaviorsProvider">
    ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
    /// </param>
    /// <param name="envelopeFactory">
    ///     The <see cref="IOutboundEnvelopeFactory" />.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="IProducerLogger{TCategoryName}" />.
    /// </param>
    protected Producer(
        string name,
        IBrokerClient client,
        ProducerEndpointConfiguration endpointConfiguration,
        IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
        IOutboundEnvelopeFactory envelopeFactory,
        IServiceProvider serviceProvider,
        IProducerLogger<IProducer> logger)
    {
        Name = Check.NotNullOrEmpty(name, nameof(name));
        EndpointConfiguration = Check.NotNull(endpointConfiguration, nameof(endpointConfiguration));
        Client = Check.NotNull(client, nameof(client));
        _behaviors = Check.NotNull(behaviorsProvider, nameof(behaviorsProvider)).GetBehaviorsList();
        _envelopeFactory = Check.NotNull(envelopeFactory, nameof(envelopeFactory));
        _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        _logger = Check.NotNull(logger, nameof(logger));
    }

    /// <inheritdoc cref="IProducer.Name" />
    public string Name { get; }

    /// <inheritdoc cref="IProducer.DisplayName" />
    public string DisplayName => Name;

    /// <summary>
    ///     Gets the related <see cref="IBrokerClient" />.
    /// </summary>
    public IBrokerClient Client { get; }

    /// <inheritdoc cref="IProducer.EndpointConfiguration" />
    public ProducerEndpointConfiguration EndpointConfiguration { get; }

    /// <inheritdoc cref="IProducer.Produce(object?,IReadOnlyCollection{MessageHeader}?)" />
    public IBrokerMessageIdentifier? Produce(
        object? message,
        IReadOnlyCollection<MessageHeader>? headers = null) =>
        Produce(
            _envelopeFactory.CreateEnvelope(
                message,
                headers,
                EndpointConfiguration.Endpoint.GetEndpoint(message, EndpointConfiguration, _serviceProvider),
                this));

    /// <inheritdoc cref="IProducer.Produce(IOutboundEnvelope)" />
    [SuppressMessage("Usage", "VSTHRD103:Call async methods when in an async method", Justification = "Method executes synchronously")]
    [SuppressMessage("Performance", "CA1849:Call async methods when in an async method", Justification = "Method executes synchronously")]
    public IBrokerMessageIdentifier? Produce(IOutboundEnvelope envelope)
    {
        try
        {
            IBrokerMessageIdentifier? brokerMessageIdentifier = null;

            ExecutePipelineAsync(
                    new ProducerPipelineContext(envelope, this, _serviceProvider),
                    finalContext =>
                    {
                        brokerMessageIdentifier = ProduceCore(finalContext.Envelope);

                        ((RawOutboundEnvelope)finalContext.Envelope).BrokerMessageIdentifier =
                            brokerMessageIdentifier;

                        _logger.LogProduced(finalContext.Envelope);

                        return ValueTaskFactory.CompletedTask;
                    })
                .SafeWait();

            return brokerMessageIdentifier;
        }
        catch (Exception ex)
        {
            _logger.LogProduceError(envelope, ex);
            throw;
        }
    }

    /// <inheritdoc cref="IProducer.Produce(object?,IReadOnlyCollection{MessageHeader}?,Action{IBrokerMessageIdentifier},Action{Exception})" />
    public void Produce(
        object? message,
        IReadOnlyCollection<MessageHeader>? headers,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        Produce(
            _envelopeFactory.CreateEnvelope(
                message,
                headers,
                EndpointConfiguration.Endpoint.GetEndpoint(message, EndpointConfiguration, _serviceProvider),
                this),
            onSuccess,
            onError);

    /// <inheritdoc cref="IProducer.Produce(IOutboundEnvelope,Action{IBrokerMessageIdentifier},Action{Exception})" />
    [SuppressMessage("Usage", "VSTHRD103:Call async methods when in an async method", Justification = "Method executes synchronously")]
    [SuppressMessage("Performance", "CA1849:Call async methods when in an async method", Justification = "Method executes synchronously")]
    public void Produce(IOutboundEnvelope envelope, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError)
    {
        try
        {
            ExecutePipelineAsync(
                    new ProducerPipelineContext(envelope, this, _serviceProvider),
                    finalContext =>
                    {
                        ProduceCore(
                            finalContext.Envelope,
                            identifier =>
                            {
                                ((RawOutboundEnvelope)finalContext.Envelope).BrokerMessageIdentifier =
                                    identifier;
                                _logger.LogProduced(finalContext.Envelope);
                                onSuccess.Invoke(identifier);
                            },
                            exception =>
                            {
                                _logger.LogProduceError(finalContext.Envelope, exception);
                                onError.Invoke(exception);
                            });
                        _logger.LogProduced(finalContext.Envelope);

                        return ValueTaskFactory.CompletedTask;
                    })
                .SafeWait();
        }
        catch (Exception ex)
        {
            _logger.LogProduceError(envelope, ex);
            throw;
        }
    }

    /// <inheritdoc cref="IProducer.RawProduce(byte[],IReadOnlyCollection{MessageHeader}?)" />
    public IBrokerMessageIdentifier? RawProduce(byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers = null) =>
        RawProduce(
            EndpointConfiguration.Endpoint.GetEndpoint(messageContent, EndpointConfiguration, _serviceProvider),
            messageContent,
            headers);

    /// <inheritdoc cref="IProducer.RawProduce(Stream?,IReadOnlyCollection{MessageHeader}?)" />
    public IBrokerMessageIdentifier? RawProduce(Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers = null) =>
        RawProduce(
            EndpointConfiguration.Endpoint.GetEndpoint(messageStream, EndpointConfiguration, _serviceProvider),
            messageStream,
            headers);

    /// <inheritdoc cref="IProducer.RawProduce(ProducerEndpoint, byte[],IReadOnlyCollection{MessageHeader}?)" />
    public IBrokerMessageIdentifier? RawProduce(
        ProducerEndpoint endpoint,
        byte[]? messageContent,
        IReadOnlyCollection<MessageHeader>? headers = null)
    {
        try
        {
            IBrokerMessageIdentifier? brokerMessageIdentifier = ProduceCore(new OutboundEnvelope(messageContent, headers, endpoint, this));

            _logger.LogProduced(endpoint, headers, brokerMessageIdentifier);

            return brokerMessageIdentifier;
        }
        catch (Exception ex)
        {
            _logger.LogProduceError(endpoint, headers, ex);
            throw;
        }
    }

    /// <inheritdoc cref="IProducer.RawProduce(ProducerEndpoint, Stream?,IReadOnlyCollection{MessageHeader}?)" />
    public IBrokerMessageIdentifier? RawProduce(
        ProducerEndpoint endpoint,
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers = null)
    {
        try
        {
            IBrokerMessageIdentifier? brokerMessageIdentifier = ProduceCore(new OutboundEnvelope(messageStream, headers, endpoint, this));

            _logger.LogProduced(endpoint, headers, brokerMessageIdentifier);

            return brokerMessageIdentifier;
        }
        catch (Exception ex)
        {
            _logger.LogProduceError(endpoint, headers, ex);
            throw;
        }
    }

    /// <inheritdoc cref="IProducer.RawProduce(byte[],IReadOnlyCollection{MessageHeader}?,Action{IBrokerMessageIdentifier},Action{Exception})" />
    public void RawProduce(
        byte[]? messageContent,
        IReadOnlyCollection<MessageHeader>? headers,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        RawProduce(
            EndpointConfiguration.Endpoint.GetEndpoint(messageContent, EndpointConfiguration, _serviceProvider),
            messageContent,
            headers,
            onSuccess,
            onError);

    /// <inheritdoc cref="IProducer.RawProduce(Stream?,IReadOnlyCollection{MessageHeader}?,Action{IBrokerMessageIdentifier},Action{Exception})" />
    public void RawProduce(
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        RawProduce(
            EndpointConfiguration.Endpoint.GetEndpoint(messageStream, EndpointConfiguration, _serviceProvider),
            messageStream,
            headers,
            onSuccess,
            onError);

    /// <inheritdoc cref="IProducer.RawProduce(ProducerEndpoint,byte[],IReadOnlyCollection{MessageHeader}?,Action{IBrokerMessageIdentifier},Action{Exception})" />
    public void RawProduce(
        ProducerEndpoint endpoint,
        byte[]? messageContent,
        IReadOnlyCollection<MessageHeader>? headers,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        ProduceCore(
            new OutboundEnvelope(messageContent, headers, endpoint, this),
            identifier =>
            {
                _logger.LogProduced(endpoint, headers, identifier);
                onSuccess.Invoke(identifier);
            },
            exception =>
            {
                _logger.LogProduceError(endpoint, headers, exception);
                onError.Invoke(exception);
            });

    /// <inheritdoc cref="IProducer.RawProduce(ProducerEndpoint,Stream,IReadOnlyCollection{MessageHeader}?,Action{IBrokerMessageIdentifier},Action{Exception})" />
    public void RawProduce(
        ProducerEndpoint endpoint,
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        ProduceCore(
            new OutboundEnvelope(messageStream, headers, endpoint, this),
            identifier =>
            {
                _logger.LogProduced(endpoint, headers, identifier);
                onSuccess.Invoke(identifier);
            },
            exception =>
            {
                _logger.LogProduceError(endpoint, headers, exception);
                onError.Invoke(exception);
            });

    /// <inheritdoc cref="IProducer.ProduceAsync(object?,IReadOnlyCollection{MessageHeader}?)" />
    public ValueTask<IBrokerMessageIdentifier?> ProduceAsync(
        object? message,
        IReadOnlyCollection<MessageHeader>? headers = null) =>
        ProduceAsync(
            _envelopeFactory.CreateEnvelope(
                message,
                headers,
                EndpointConfiguration.Endpoint.GetEndpoint(message, EndpointConfiguration, _serviceProvider),
                this));

    /// <inheritdoc cref="IProducer.ProduceAsync(IOutboundEnvelope)" />
    public async ValueTask<IBrokerMessageIdentifier?> ProduceAsync(IOutboundEnvelope envelope)
    {
        try
        {
            IBrokerMessageIdentifier? brokerMessageIdentifier = null;

            await ExecutePipelineAsync(
                new ProducerPipelineContext(envelope, this, _serviceProvider),
                async finalContext =>
                {
                    brokerMessageIdentifier = await ProduceCoreAsync(finalContext.Envelope).ConfigureAwait(false);

                    ((RawOutboundEnvelope)finalContext.Envelope).BrokerMessageIdentifier = brokerMessageIdentifier;

                    _logger.LogProduced(finalContext.Envelope);
                }).ConfigureAwait(false);

            return brokerMessageIdentifier;
        }
        catch (Exception ex)
        {
            _logger.LogProduceError(envelope, ex);
            throw;
        }
    }

    /// <inheritdoc cref="IProducer.RawProduceAsync(byte[],IReadOnlyCollection{MessageHeader}?)" />
    public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers = null) =>
        RawProduceAsync(
            EndpointConfiguration.Endpoint.GetEndpoint(messageContent, EndpointConfiguration, _serviceProvider),
            messageContent,
            headers);

    /// <inheritdoc cref="IProducer.RawProduceAsync(Stream?,IReadOnlyCollection{MessageHeader}?)" />
    public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers = null) =>
        RawProduceAsync(
            EndpointConfiguration.Endpoint.GetEndpoint(messageStream, EndpointConfiguration, _serviceProvider),
            messageStream,
            headers);

    /// <inheritdoc cref="IProducer.RawProduceAsync(ProducerEndpoint, byte[],IReadOnlyCollection{MessageHeader}?)" />
    public async ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(
        ProducerEndpoint endpoint,
        byte[]? messageContent,
        IReadOnlyCollection<MessageHeader>? headers = null)
    {
        try
        {
            IBrokerMessageIdentifier? brokerMessageIdentifier = await ProduceCoreAsync(new OutboundEnvelope(messageContent, headers, endpoint, this)).ConfigureAwait(false);

            _logger.LogProduced(endpoint, headers, brokerMessageIdentifier);

            return brokerMessageIdentifier;
        }
        catch (Exception ex)
        {
            _logger.LogProduceError(endpoint, headers, ex);
            throw;
        }
    }

    /// <inheritdoc cref="IProducer.RawProduceAsync(ProducerEndpoint, Stream?,IReadOnlyCollection{MessageHeader}?)" />
    public async ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(
        ProducerEndpoint endpoint,
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers = null)
    {
        try
        {
            IBrokerMessageIdentifier? brokerMessageIdentifier = await ProduceCoreAsync(new OutboundEnvelope(messageStream, headers, endpoint, this)).ConfigureAwait(false);

            _logger.LogProduced(endpoint, headers, brokerMessageIdentifier);

            return brokerMessageIdentifier;
        }
        catch (Exception ex)
        {
            _logger.LogProduceError(endpoint, headers, ex);
            throw;
        }
    }

    /// <inheritdoc cref="IDisposable.Dispose" />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Publishes the specified message and returns its identifier.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message to be produced.
    /// </param>
    /// <returns>
    ///     The message identifier assigned by the broker (the Kafka offset or similar).
    /// </returns>
    protected abstract IBrokerMessageIdentifier? ProduceCore(IOutboundEnvelope envelope);

    /// <summary>
    ///     Publishes the specified message and returns its identifier.
    /// </summary>
    /// <remarks>
    ///     In this implementation the message is synchronously enqueued but produced asynchronously. The callbacks
    ///     are called when the message is actually produced (or the produce failed).
    /// </remarks>
    /// <param name="envelope">
    ///     The envelope containing the message to be produced.
    /// </param>
    /// <param name="onSuccess">
    ///     The callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     The callback to be invoked when the produce fails.
    /// </param>
    protected abstract void ProduceCore(IOutboundEnvelope envelope, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError);

    /// <summary>
    ///     Publishes the specified message and returns its identifier.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message to be produced.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The task result contains the
    ///     message identifier assigned by the broker (the Kafka offset or similar).
    /// </returns>
    protected abstract ValueTask<IBrokerMessageIdentifier?> ProduceCoreAsync(IOutboundEnvelope envelope);

    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged  resources.
    /// </summary>
    /// <param name="disposing">
    ///     A value indicating whether the method has been called by the <c>Dispose</c> method and not from the finalizer.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing || _isDisposed)
            return;

        _isDisposed = true;
    }

    private ValueTask ExecutePipelineAsync(
        ProducerPipelineContext context,
        ProducerBehaviorHandler finalAction,
        int stepIndex = 0)
    {
        if (stepIndex >= _behaviors.Count)
            return finalAction(context);

        // TODO: Can get rid of this delegate allocation?
        return _behaviors[stepIndex].HandleAsync(
            context,
            nextContext => ExecutePipelineAsync(nextContext, finalAction, stepIndex + 1));
    }
}
