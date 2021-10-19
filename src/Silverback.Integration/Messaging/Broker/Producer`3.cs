// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

/// <inheritdoc cref="IProducer" />
/// <typeparam name="TBroker">
///     The type of the related <see cref="IBroker" /> implementation.
/// </typeparam>
/// <typeparam name="TConfiguration">
///     The type of the <see cref="ProducerConfiguration" /> used by the producer implementation.
/// </typeparam>
/// <typeparam name="TEndpoint">
///     The type of the <see cref="ProducerEndpoint" /> used by the producer implementation.
/// </typeparam>
[SuppressMessage("", "CA1005", Justification = Justifications.NoWayToReduceTypeParameters)]
public abstract class Producer<TBroker, TConfiguration, TEndpoint> : IProducer
    where TBroker : IBroker
    where TConfiguration : ProducerConfiguration
    where TEndpoint : ProducerEndpoint
{
    private readonly IReadOnlyList<IProducerBehavior> _behaviors;

    private readonly IServiceProvider _serviceProvider;

    private readonly IOutboundLogger<IProducer> _logger;

    private readonly IOutboundEnvelopeFactory _envelopeFactory;

    private Task? _connectTask;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Producer{TBroker,TEndpoint,TEndpoint}" /> class.
    /// </summary>
    /// <param name="broker">
    ///     The <see cref="IBroker" /> that instantiated this producer.
    /// </param>
    /// <param name="configuration">
    ///     The <see cref="TConfiguration" />.
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
    ///     The <see cref="IOutboundLogger{TCategoryName}" />.
    /// </param>
    protected Producer(
        TBroker broker,
        TConfiguration configuration,
        IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
        IOutboundEnvelopeFactory envelopeFactory,
        IServiceProvider serviceProvider,
        IOutboundLogger<IProducer> logger)
    {
        Broker = Check.NotNull(broker, nameof(broker));
        Configuration = Check.NotNull(configuration, nameof(configuration));
        _behaviors = Check.NotNull(behaviorsProvider, nameof(behaviorsProvider)).GetBehaviorsList();
        _envelopeFactory = Check.NotNull(envelopeFactory, nameof(envelopeFactory));
        _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        _logger = Check.NotNull(logger, nameof(logger));

        Configuration.Validate();
    }

    /// <inheritdoc cref="IBrokerConnectedObject.Id" />
    public InstanceIdentifier Id { get; } = new();

    /// <inheritdoc cref="IBrokerConnectedObject.Broker" />
    public TBroker Broker { get; }

    /// <inheritdoc cref="IProducer.Configuration" />
    public TConfiguration Configuration { get; }

    /// <inheritdoc cref="IBrokerConnectedObject.IsConnecting" />
    public bool IsConnecting => _connectTask != null;

    /// <inheritdoc cref="IBrokerConnectedObject.IsConnected" />
    public bool IsConnected { get; private set; }

    /// <inheritdoc cref="IBrokerConnectedObject.Broker" />
    IBroker IBrokerConnectedObject.Broker => Broker;

    /// <inheritdoc cref="IProducer.Configuration" />
    ProducerConfiguration IProducer.Configuration => Configuration;

    /// <inheritdoc cref="IBrokerConnectedObject.ConnectAsync" />
    public async Task ConnectAsync()
    {
        if (IsConnected)
            return;

        if (_connectTask != null)
        {
            await _connectTask.ConfigureAwait(false);
            return;
        }

        _connectTask = ConnectCoreAsync();

        try
        {
            await _connectTask.ConfigureAwait(false);

            IsConnected = true;
        }
        finally
        {
            _connectTask = null;
        }

        _logger.LogProducerConnected(this);
    }

    /// <inheritdoc cref="IBrokerConnectedObject.DisconnectAsync" />
    public async Task DisconnectAsync()
    {
        if (!IsConnected)
            return;

        await DisconnectCoreAsync().ConfigureAwait(false);

        IsConnected = false;
        _logger.LogProducerDisconnected(this);
    }

    /// <inheritdoc cref="IProducer.Produce(object?,IReadOnlyCollection{MessageHeader}?)" />
    public IBrokerMessageIdentifier? Produce(
        object? message,
        IReadOnlyCollection<MessageHeader>? headers = null) =>
        Produce(
            _envelopeFactory.CreateEnvelope(
                message,
                headers,
                Configuration.Endpoint.GetEndpoint(message, Configuration, _serviceProvider)));

    /// <inheritdoc cref="IProducer.Produce(IOutboundEnvelope)" />
    [SuppressMessage("", "VSTHRD103", Justification = "Method executes synchronously")]
    public IBrokerMessageIdentifier? Produce(IOutboundEnvelope envelope)
    {
        try
        {
            IBrokerMessageIdentifier? brokerMessageIdentifier = null;

            AsyncHelper.RunSynchronously(
                async () =>
                    await ExecutePipelineAsync(
                        new ProducerPipelineContext(envelope, this, _serviceProvider),
                        finalContext =>
                        {
                            brokerMessageIdentifier = ProduceCore(
                                finalContext.Envelope.Message,
                                finalContext.Envelope.RawMessage,
                                finalContext.Envelope.Headers,
                                (TEndpoint)finalContext.Envelope.Endpoint);

                            ((RawOutboundEnvelope)finalContext.Envelope).BrokerMessageIdentifier =
                                brokerMessageIdentifier;

                            _logger.LogProduced(envelope);

                            return Task.CompletedTask;
                        }).ConfigureAwait(false));

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
                Configuration.Endpoint.GetEndpoint(message, Configuration, _serviceProvider)),
            onSuccess,
            onError);

    /// <inheritdoc cref="IProducer.Produce(IOutboundEnvelope,Action{IBrokerMessageIdentifier},Action{Exception})" />
    [SuppressMessage("", "VSTHRD103", Justification = "OK to call sync ProduceCore")]
    public void Produce(
        IOutboundEnvelope envelope,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError)
    {
        try
        {
            AsyncHelper.RunSynchronously(
                async () =>
                {
                    await ExecutePipelineAsync(
                        new ProducerPipelineContext(envelope, this, _serviceProvider),
                        finalContext =>
                        {
                            ProduceCore(
                                finalContext.Envelope.Message,
                                finalContext.Envelope.RawMessage,
                                finalContext.Envelope.Headers,
                                (TEndpoint)finalContext.Envelope.Endpoint,
                                identifier =>
                                {
                                    ((RawOutboundEnvelope)finalContext.Envelope).BrokerMessageIdentifier =
                                        identifier;
                                    _logger.LogProduced(envelope);
                                    onSuccess.Invoke(identifier);
                                },
                                exception =>
                                {
                                    _logger.LogProduceError(envelope, exception);
                                    onError.Invoke(exception);
                                });
                            _logger.LogProduced(envelope);

                            return Task.CompletedTask;
                        }).ConfigureAwait(false);
                });
        }
        catch (Exception ex)
        {
            _logger.LogProduceError(envelope, ex);
            throw;
        }
    }

    /// <inheritdoc cref="IProducer.RawProduce(byte[],IReadOnlyCollection{MessageHeader}?)" />
    public IBrokerMessageIdentifier? RawProduce(
        byte[]? messageContent,
        IReadOnlyCollection<MessageHeader>? headers = null) =>
        RawProduce(
            Configuration.Endpoint.GetEndpoint(messageContent, Configuration, _serviceProvider),
            messageContent,
            headers);

    /// <inheritdoc cref="IProducer.RawProduce(Stream?,IReadOnlyCollection{MessageHeader}?)" />
    public IBrokerMessageIdentifier? RawProduce(
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers = null) =>
        RawProduce(
            Configuration.Endpoint.GetEndpoint(messageStream, Configuration, _serviceProvider),
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
            IBrokerMessageIdentifier? brokerMessageIdentifier = ProduceCore(
                null,
                messageContent,
                headers,
                (TEndpoint)endpoint);

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
            IBrokerMessageIdentifier? brokerMessageIdentifier = ProduceCore(
                null,
                messageStream,
                headers,
                (TEndpoint)endpoint);

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
            Configuration.Endpoint.GetEndpoint(messageContent, Configuration, _serviceProvider),
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
            Configuration.Endpoint.GetEndpoint(messageStream, Configuration, _serviceProvider),
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
            null,
            messageContent,
            headers,
            (TEndpoint)endpoint,
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
            null,
            messageStream,
            headers,
            (TEndpoint)endpoint,
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
    public async Task<IBrokerMessageIdentifier?> ProduceAsync(
        object? message,
        IReadOnlyCollection<MessageHeader>? headers = null) =>
        await ProduceAsync(
                _envelopeFactory.CreateEnvelope(
                    message,
                    headers,
                    Configuration.Endpoint.GetEndpoint(message, Configuration, _serviceProvider)))
            .ConfigureAwait(false);

    /// <inheritdoc cref="IProducer.ProduceAsync(IOutboundEnvelope)" />
    public async Task<IBrokerMessageIdentifier?> ProduceAsync(IOutboundEnvelope envelope)
    {
        try
        {
            IBrokerMessageIdentifier? brokerMessageIdentifier = null;

            await ExecutePipelineAsync(
                new ProducerPipelineContext(envelope, this, _serviceProvider),
                async finalContext =>
                {
                    brokerMessageIdentifier = await ProduceCoreAsync(
                            finalContext.Envelope.Message,
                            finalContext.Envelope.RawMessage,
                            finalContext.Envelope.Headers,
                            (TEndpoint)finalContext.Envelope.Endpoint)
                        .ConfigureAwait(false);

                    ((RawOutboundEnvelope)finalContext.Envelope).BrokerMessageIdentifier =
                        brokerMessageIdentifier;

                    _logger.LogProduced(envelope);
                }).ConfigureAwait(false);

            return brokerMessageIdentifier;
        }
        catch (Exception ex)
        {
            _logger.LogProduceError(envelope, ex);
            throw;
        }
    }

    /// <inheritdoc cref="IProducer.ProduceAsync(object?,IReadOnlyCollection{MessageHeader}?,Action{IBrokerMessageIdentifier},Action{Exception})" />
    public Task ProduceAsync(
        object? message,
        IReadOnlyCollection<MessageHeader>? headers,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        ProduceAsync(
            _envelopeFactory.CreateEnvelope(
                message,
                headers,
                Configuration.Endpoint.GetEndpoint(message, Configuration, _serviceProvider)),
            onSuccess,
            onError);

    /// <inheritdoc cref="IProducer.ProduceAsync(IOutboundEnvelope,Action{IBrokerMessageIdentifier},Action{Exception})" />
    public async Task ProduceAsync(
        IOutboundEnvelope envelope,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        await ExecutePipelineAsync(
            new ProducerPipelineContext(envelope, this, _serviceProvider),
            finalContext => ProduceCoreAsync(
                finalContext.Envelope.Message,
                finalContext.Envelope.RawMessage,
                finalContext.Envelope.Headers,
                (TEndpoint)finalContext.Envelope.Endpoint,
                identifier =>
                {
                    ((RawOutboundEnvelope)finalContext.Envelope).BrokerMessageIdentifier = identifier;
                    _logger.LogProduced(envelope);
                    onSuccess.Invoke(identifier);
                },
                exception =>
                {
                    _logger.LogProduceError(envelope, exception);
                    onError.Invoke(exception);
                })).ConfigureAwait(false);

    /// <inheritdoc cref="IProducer.RawProduceAsync(byte[],IReadOnlyCollection{MessageHeader}?)" />
    public Task<IBrokerMessageIdentifier?> RawProduceAsync(
        byte[]? messageContent,
        IReadOnlyCollection<MessageHeader>? headers = null) =>
        RawProduceAsync(
            Configuration.Endpoint.GetEndpoint(messageContent, Configuration, _serviceProvider),
            messageContent,
            headers);

    /// <inheritdoc cref="IProducer.RawProduceAsync(Stream?,IReadOnlyCollection{MessageHeader}?)" />
    public Task<IBrokerMessageIdentifier?> RawProduceAsync(
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers = null) =>
        RawProduceAsync(
            Configuration.Endpoint.GetEndpoint(messageStream, Configuration, _serviceProvider),
            messageStream,
            headers);

    /// <inheritdoc cref="IProducer.RawProduceAsync(ProducerEndpoint, byte[],IReadOnlyCollection{MessageHeader}?)" />
    public async Task<IBrokerMessageIdentifier?> RawProduceAsync(
        ProducerEndpoint endpoint,
        byte[]? messageContent,
        IReadOnlyCollection<MessageHeader>? headers = null)
    {
        try
        {
            IBrokerMessageIdentifier? brokerMessageIdentifier = await ProduceCoreAsync(
                    null,
                    messageContent,
                    headers,
                    (TEndpoint)endpoint)
                .ConfigureAwait(false);

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
    public async Task<IBrokerMessageIdentifier?> RawProduceAsync(
        ProducerEndpoint endpoint,
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers = null)
    {
        try
        {
            IBrokerMessageIdentifier? brokerMessageIdentifier = await ProduceCoreAsync(
                    null,
                    messageStream,
                    headers,
                    (TEndpoint)endpoint)
                .ConfigureAwait(false);

            _logger.LogProduced(endpoint, headers, brokerMessageIdentifier);

            return brokerMessageIdentifier;
        }
        catch (Exception ex)
        {
            _logger.LogProduceError(endpoint, headers, ex);
            throw;
        }
    }

    /// <inheritdoc cref="IProducer.RawProduceAsync(byte[],IReadOnlyCollection{MessageHeader}?)" />
    public Task RawProduceAsync(
        byte[]? messageContent,
        IReadOnlyCollection<MessageHeader>? headers,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        RawProduceAsync(
            Configuration.Endpoint.GetEndpoint(messageContent, Configuration, _serviceProvider),
            messageContent,
            headers,
            onSuccess,
            onError);

    /// <inheritdoc cref="IProducer.RawProduceAsync(Stream?,IReadOnlyCollection{MessageHeader}?)" />
    public Task RawProduceAsync(
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        RawProduceAsync(
            Configuration.Endpoint.GetEndpoint(messageStream, Configuration, _serviceProvider),
            messageStream,
            headers,
            onSuccess,
            onError);

    /// <inheritdoc cref="IProducer.RawProduceAsync(ProducerEndpoint,byte[],IReadOnlyCollection{MessageHeader}?)" />
    public Task RawProduceAsync(
        ProducerEndpoint endpoint,
        byte[]? messageContent,
        IReadOnlyCollection<MessageHeader>? headers,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        ProduceCoreAsync(
            null,
            messageContent,
            headers,
            (TEndpoint)endpoint,
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

    /// <inheritdoc cref="IProducer.RawProduceAsync(ProducerEndpoint,Stream?,IReadOnlyCollection{MessageHeader}?)" />
    public Task RawProduceAsync(
        ProducerEndpoint endpoint,
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        ProduceCoreAsync(
            null,
            messageStream,
            headers,
            (TEndpoint)endpoint,
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

    /// <summary>
    ///     Connects to the message broker.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    protected virtual Task ConnectCoreAsync() => Task.CompletedTask;

    /// <summary>
    ///     Disconnects from the message broker.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    protected virtual Task DisconnectCoreAsync() => Task.CompletedTask;

    /// <summary>
    ///     Publishes the specified message and returns its identifier.
    /// </summary>
    /// <param name="message">
    ///     The message to be delivered before serialization. This might be null if
    ///     <see cref="RawProduce(byte[],IReadOnlyCollection{MessageHeader})" />,
    ///     <see cref="RawProduce(Stream,IReadOnlyCollection{MessageHeader})" />,
    ///     <see cref="RawProduceAsync(byte[],IReadOnlyCollection{MessageHeader})" /> or
    ///     <see cref="RawProduceAsync(Stream,IReadOnlyCollection{MessageHeader})" /> have been used to
    ///     produce.
    /// </param>
    /// <param name="messageStream">
    ///     The actual serialized message to be delivered.
    /// </param>
    /// <param name="headers">
    ///     The message headers.
    /// </param>
    /// <param name="endpoint">
    ///     The actual endpoint to produce to.
    /// </param>
    /// <returns>
    ///     The message identifier assigned by the broker (the Kafka offset or similar).
    /// </returns>
    protected abstract IBrokerMessageIdentifier? ProduceCore(
        object? message,
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers,
        TEndpoint endpoint);

    /// <summary>
    ///     Publishes the specified message and returns its identifier.
    /// </summary>
    /// <param name="message">
    ///     The message to be delivered before serialization. This might be null if
    ///     <see cref="RawProduce(byte[],IReadOnlyCollection{MessageHeader})" />,
    ///     <see cref="RawProduce(Stream,IReadOnlyCollection{MessageHeader})" />,
    ///     <see cref="RawProduceAsync(byte[],IReadOnlyCollection{MessageHeader})" /> or
    ///     <see cref="RawProduceAsync(Stream,IReadOnlyCollection{MessageHeader})" /> have been used to
    ///     produce.
    /// </param>
    /// <param name="messageBytes">
    ///     The actual serialized message to be delivered.
    /// </param>
    /// <param name="headers">
    ///     The message headers.
    /// </param>
    /// <param name="endpoint">
    ///     The actual endpoint to produce to.
    /// </param>
    /// <returns>
    ///     The message identifier assigned by the broker (the Kafka offset or similar).
    /// </returns>
    protected abstract IBrokerMessageIdentifier? ProduceCore(
        object? message,
        byte[]? messageBytes,
        IReadOnlyCollection<MessageHeader>? headers,
        TEndpoint endpoint);

    /// <summary>
    ///     Publishes the specified message and returns its identifier.
    /// </summary>
    /// <remarks>
    ///     In this implementation the message is synchronously enqueued but produced asynchronously. The callbacks
    ///     are called when the message is actually produced (or the produce failed).
    /// </remarks>
    /// <param name="message">
    ///     The message to be delivered before serialization. This might be null if
    ///     <see cref="RawProduce(byte[],IReadOnlyCollection{MessageHeader})" />,
    ///     <see cref="RawProduce(Stream,IReadOnlyCollection{MessageHeader})" />,
    ///     <see cref="RawProduceAsync(byte[],IReadOnlyCollection{MessageHeader})" /> or
    ///     <see cref="RawProduceAsync(Stream,IReadOnlyCollection{MessageHeader})" /> have been used to
    ///     produce.
    /// </param>
    /// <param name="messageStream">
    ///     The message to be delivered.
    /// </param>
    /// <param name="headers">
    ///     The message headers.
    /// </param>
    /// <param name="endpoint">
    ///     The actual endpoint to produce to.
    /// </param>
    /// <param name="onSuccess">
    ///     The callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     The callback to be invoked when the produce fails.
    /// </param>
    protected abstract void ProduceCore(
        object? message,
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers,
        TEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError);

    /// <summary>
    ///     Publishes the specified message and returns its identifier.
    /// </summary>
    /// <remarks>
    ///     In this implementation the message is synchronously enqueued but produced asynchronously. The callbacks
    ///     are called when the message is actually produced (or the produce failed).
    /// </remarks>
    /// <param name="message">
    ///     The message to be delivered before serialization. This might be null if
    ///     <see cref="RawProduce(byte[],IReadOnlyCollection{MessageHeader})" />,
    ///     <see cref="RawProduce(Stream,IReadOnlyCollection{MessageHeader})" />,
    ///     <see cref="RawProduceAsync(byte[],IReadOnlyCollection{MessageHeader})" /> or
    ///     <see cref="RawProduceAsync(Stream,IReadOnlyCollection{MessageHeader})" /> have been used to
    ///     produce.
    /// </param>
    /// <param name="messageBytes">
    ///     The actual serialized message to be delivered.
    /// </param>
    /// <param name="headers">
    ///     The message headers.
    /// </param>
    /// <param name="endpoint">
    ///     The actual endpoint to produce to.
    /// </param>
    /// <param name="onSuccess">
    ///     The callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     The callback to be invoked when the produce fails.
    /// </param>
    protected abstract void ProduceCore(
        object? message,
        byte[]? messageBytes,
        IReadOnlyCollection<MessageHeader>? headers,
        TEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError);

    /// <summary>
    ///     Publishes the specified message and returns its identifier.
    /// </summary>
    /// <param name="message">
    ///     The message to be delivered before serialization. This might be null if
    ///     <see cref="RawProduce(byte[],IReadOnlyCollection{MessageHeader})" />,
    ///     <see cref="RawProduce(Stream,IReadOnlyCollection{MessageHeader})" />,
    ///     <see cref="RawProduceAsync(byte[],IReadOnlyCollection{MessageHeader})" /> or
    ///     <see cref="RawProduceAsync(Stream,IReadOnlyCollection{MessageHeader})" /> have been used to
    ///     produce.
    /// </param>
    /// <param name="messageStream">
    ///     The message to be delivered.
    /// </param>
    /// <param name="headers">
    ///     The message headers.
    /// </param>
    /// <param name="endpoint">
    ///     The actual endpoint to produce to.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
    ///     message identifier assigned by the broker (the Kafka offset or similar).
    /// </returns>
    protected abstract Task<IBrokerMessageIdentifier?> ProduceCoreAsync(
        object? message,
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers,
        TEndpoint endpoint);

    /// <summary>
    ///     Publishes the specified message and returns its identifier.
    /// </summary>
    /// <param name="message">
    ///     The message to be delivered before serialization. This might be null if
    ///     <see cref="RawProduce(byte[],IReadOnlyCollection{MessageHeader})" />,
    ///     <see cref="RawProduce(Stream,IReadOnlyCollection{MessageHeader})" />,
    ///     <see cref="RawProduceAsync(byte[],IReadOnlyCollection{MessageHeader})" /> or
    ///     <see cref="RawProduceAsync(Stream,IReadOnlyCollection{MessageHeader})" /> have been used to
    ///     produce.
    /// </param>
    /// <param name="messageBytes">
    ///     The actual serialized message to be delivered.
    /// </param>
    /// <param name="headers">
    ///     The message headers.
    /// </param>
    /// <param name="endpoint">
    ///     The actual endpoint to produce to.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
    ///     message identifier assigned by the broker (the Kafka offset or similar).
    /// </returns>
    protected abstract Task<IBrokerMessageIdentifier?> ProduceCoreAsync(
        object? message,
        byte[]? messageBytes,
        IReadOnlyCollection<MessageHeader>? headers,
        TEndpoint endpoint);

    /// <summary>
    ///     Publishes the specified message and returns its identifier.
    /// </summary>
    /// <remarks>
    ///     The returned <see cref="Task" /> completes when the message is enqueued while the callbacks
    ///     are called when the message is actually produced (or the produce failed).
    /// </remarks>
    /// <param name="message">
    ///     The message to be delivered before serialization. This might be null if
    ///     <see cref="RawProduce(byte[],IReadOnlyCollection{MessageHeader})" />,
    ///     <see cref="RawProduce(Stream,IReadOnlyCollection{MessageHeader})" />,
    ///     <see cref="RawProduceAsync(byte[],IReadOnlyCollection{MessageHeader})" /> or
    ///     <see cref="RawProduceAsync(Stream,IReadOnlyCollection{MessageHeader})" /> have been used to
    ///     produce.
    /// </param>
    /// <param name="messageStream">
    ///     The message to be delivered.
    /// </param>
    /// <param name="headers">
    ///     The message headers.
    /// </param>
    /// <param name="endpoint">
    ///     The actual endpoint to produce to.
    /// </param>
    /// <param name="onSuccess">
    ///     The callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     The callback to be invoked when the produce fails.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation. The <see cref="Task" /> will complete as
    ///     soon as the message is enqueued.
    /// </returns>
    protected abstract Task ProduceCoreAsync(
        object? message,
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers,
        TEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError);

    /// <summary>
    ///     Publishes the specified message and returns its identifier.
    /// </summary>
    /// <remarks>
    ///     The returned <see cref="Task" /> completes when the message is enqueued while the callbacks
    ///     are called when the message is actually produced (or the produce failed).
    /// </remarks>
    /// <param name="message">
    ///     The message to be delivered before serialization. This might be null if
    ///     <see cref="RawProduce(byte[],IReadOnlyCollection{MessageHeader})" />,
    ///     <see cref="RawProduce(Stream,IReadOnlyCollection{MessageHeader})" />,
    ///     <see cref="RawProduceAsync(byte[],IReadOnlyCollection{MessageHeader})" /> or
    ///     <see cref="RawProduceAsync(Stream,IReadOnlyCollection{MessageHeader})" /> have been used to
    ///     produce.
    /// </param>
    /// <param name="messageBytes">
    ///     The actual serialized message to be delivered.
    /// </param>
    /// <param name="headers">
    ///     The message headers.
    /// </param>
    /// <param name="endpoint">
    ///     The actual endpoint to produce to.
    /// </param>
    /// <param name="onSuccess">
    ///     The callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     The callback to be invoked when the produce fails.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation. The <see cref="Task" /> will complete as
    ///     soon as the message is enqueued.
    /// </returns>
    protected abstract Task ProduceCoreAsync(
        object? message,
        byte[]? messageBytes,
        IReadOnlyCollection<MessageHeader>? headers,
        TEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError);

    private Task ExecutePipelineAsync(
        ProducerPipelineContext context,
        ProducerBehaviorHandler finalAction,
        int stepIndex = 0)
    {
        if (_behaviors.Count <= 0 || stepIndex >= _behaviors.Count)
            return finalAction(context);

        return _behaviors[stepIndex].HandleAsync(
            context,
            nextContext => ExecutePipelineAsync(nextContext, finalAction, stepIndex + 1));
    }
}
