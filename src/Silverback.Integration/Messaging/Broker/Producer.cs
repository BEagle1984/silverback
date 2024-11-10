// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
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
        IServiceProvider serviceProvider,
        IProducerLogger<IProducer> logger)
    {
        Name = Check.NotNullOrEmpty(name, nameof(name));
        EndpointConfiguration = Check.NotNull(endpointConfiguration, nameof(endpointConfiguration));
        Client = Check.NotNull(client, nameof(client));
        _behaviors = Check.NotNull(behaviorsProvider, nameof(behaviorsProvider)).GetBehaviorsList();
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
        Produce(OutboundEnvelopeFactory.CreateEnvelope(message, headers, EndpointConfiguration, this));

    /// <inheritdoc cref="IProducer.Produce(IOutboundEnvelope)" />
    public IBrokerMessageIdentifier? Produce(IOutboundEnvelope envelope)
    {
        try
        {
            ProducerPipelineContext context = new(
                envelope,
                this,
                _behaviors,
                static (finalContext, _) =>
                {
                    ((RawOutboundEnvelope)finalContext.Envelope).BrokerMessageIdentifier =
                        finalContext.BrokerMessageIdentifier =
                            ((Producer)finalContext.Producer).ProduceCore(finalContext.Envelope);

                    finalContext.ServiceProvider.GetRequiredService<IProducerLogger<IProducer>>().LogProduced(finalContext.Envelope);

                    return ValueTaskFactory.CompletedTask;
                },
                _serviceProvider);

            ExecutePipelineAsync(context, CancellationToken.None).SafeWait();

            return context.BrokerMessageIdentifier;
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
        Produce(OutboundEnvelopeFactory.CreateEnvelope(message, headers, EndpointConfiguration, this), onSuccess, onError);

    /// <inheritdoc cref="IProducer.Produce{TState}(object?,IReadOnlyCollection{MessageHeader}?,Action{IBrokerMessageIdentifier,TState},Action{Exception,TState},TState)" />
    public void Produce<TState>(
        object? message,
        IReadOnlyCollection<MessageHeader>? headers,
        Action<IBrokerMessageIdentifier?, TState> onSuccess,
        Action<Exception, TState> onError,
        TState state) =>
        Produce(OutboundEnvelopeFactory.CreateEnvelope(message, headers, EndpointConfiguration, this), onSuccess, onError, state);

    /// <inheritdoc cref="IProducer.Produce(IOutboundEnvelope,Action{IBrokerMessageIdentifier},Action{Exception})" />
    public void Produce(IOutboundEnvelope envelope, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) =>
        Produce(
            envelope,
            static (identifier, state) => state.OnSuccess.Invoke(identifier),
            static (exception, state) => state.OnError.Invoke(exception),
            (OnSuccess: onSuccess, OnError: onError));

    /// <inheritdoc cref="IProducer.Produce{TState}(IOutboundEnvelope,Action{IBrokerMessageIdentifier,TState},Action{Exception,TState},TState)" />
    public void Produce<TState>(
        IOutboundEnvelope envelope,
        Action<IBrokerMessageIdentifier?, TState> onSuccess,
        Action<Exception, TState> onError,
        TState state)
    {
        try
        {
            ProducerPipelineContext<TState> context = new(
                envelope,
                this,
                _behaviors,
                static (finalContext, _) =>
                {
                    static void OnSuccess(IBrokerMessageIdentifier? identifier, ProducerPipelineContext finalContext)
                    {
                        ProducerPipelineContext<TState> finalContextWithCallbacks = (ProducerPipelineContext<TState>)finalContext;
                        ((RawOutboundEnvelope)finalContext.Envelope).BrokerMessageIdentifier = identifier;
                        finalContext.ServiceProvider.GetRequiredService<IProducerLogger<IProducer>>().LogProduced(finalContext.Envelope);
                        finalContextWithCallbacks.OnSuccess!.Invoke(identifier, finalContextWithCallbacks.CallbackState!);
                    }

                    static void OnError(Exception exception, ProducerPipelineContext finalContext)
                    {
                        ProducerPipelineContext<TState> finalContextWithCallbacks = (ProducerPipelineContext<TState>)finalContext;
                        finalContext.ServiceProvider.GetRequiredService<IProducerLogger<IProducer>>().LogProduceError(finalContext.Envelope, exception);
                        finalContextWithCallbacks.OnError!.Invoke(exception, finalContextWithCallbacks.CallbackState!);
                    }

                    ((Producer)finalContext.Producer).ProduceCore(
                        finalContext.Envelope,
                        OnSuccess,
                        OnError,
                        finalContext);

                    return ValueTaskFactory.CompletedTask;
                },
                _serviceProvider)
            {
                OnSuccess = onSuccess,
                OnError = onError,
                CallbackState = state
            };

            ExecutePipelineAsync(context, CancellationToken.None).SafeWait();
        }
        catch (Exception ex)
        {
            _logger.LogProduceError(envelope, ex);
            throw;
        }
    }

    /// <inheritdoc cref="IProducer.RawProduce(byte[],IReadOnlyCollection{MessageHeader}?)" />
    public IBrokerMessageIdentifier? RawProduce(byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers = null)
    {
        try
        {
            OutboundEnvelope envelope = new(messageContent, headers, EndpointConfiguration, this);
            IBrokerMessageIdentifier? brokerMessageIdentifier = ProduceCore(envelope);

            _logger.LogProduced(EndpointConfiguration, headers, brokerMessageIdentifier);

            return brokerMessageIdentifier;
        }
        catch (Exception ex)
        {
            _logger.LogProduceError(EndpointConfiguration, headers, ex);
            throw;
        }
    }

    /// <inheritdoc cref="IProducer.RawProduce(Stream?,IReadOnlyCollection{MessageHeader}?)" />
    public IBrokerMessageIdentifier? RawProduce(Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers = null)
    {
        try
        {
            OutboundEnvelope envelope = new(messageStream, headers, EndpointConfiguration, this);
            IBrokerMessageIdentifier? brokerMessageIdentifier = ProduceCore(envelope);

            _logger.LogProduced(EndpointConfiguration, headers, brokerMessageIdentifier);

            return brokerMessageIdentifier;
        }
        catch (Exception ex)
        {
            _logger.LogProduceError(EndpointConfiguration, headers, ex);
            throw;
        }
    }

    /// <inheritdoc cref="IProducer.RawProduce(byte[],IReadOnlyCollection{MessageHeader}?,Action{IBrokerMessageIdentifier},Action{Exception})" />
    public void RawProduce(
        byte[]? messageContent,
        IReadOnlyCollection<MessageHeader>? headers,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        RawProduce(new OutboundEnvelope(messageContent, headers, EndpointConfiguration, this), onSuccess, onError);

    /// <inheritdoc cref="IProducer.RawProduce{TState}(byte[],IReadOnlyCollection{MessageHeader}?,Action{IBrokerMessageIdentifier,TState},Action{Exception,TState},TState)" />
    public void RawProduce<TState>(
        byte[]? messageContent,
        IReadOnlyCollection<MessageHeader>? headers,
        Action<IBrokerMessageIdentifier?, TState> onSuccess,
        Action<Exception, TState> onError,
        TState state) =>
        RawProduce(new OutboundEnvelope(messageContent, headers, EndpointConfiguration, this), onSuccess, onError, state);

    /// <inheritdoc cref="IProducer.RawProduce(Stream?,IReadOnlyCollection{MessageHeader}?,Action{IBrokerMessageIdentifier},Action{Exception})" />
    public void RawProduce(
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        RawProduce(new OutboundEnvelope(messageStream, headers, EndpointConfiguration, this), onSuccess, onError);

    /// <inheritdoc cref="IProducer.RawProduce{TState}(Stream?,IReadOnlyCollection{MessageHeader}?,Action{IBrokerMessageIdentifier,TState},Action{Exception,TState},TState)" />
    public void RawProduce<TState>(
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers,
        Action<IBrokerMessageIdentifier?, TState> onSuccess,
        Action<Exception, TState> onError,
        TState state) =>
        RawProduce(new OutboundEnvelope(messageStream, headers, EndpointConfiguration, this), onSuccess, onError, state);

    /// <inheritdoc cref="IProducer.ProduceAsync(object?,IReadOnlyCollection{MessageHeader}?,CancellationToken)" />
    public ValueTask<IBrokerMessageIdentifier?> ProduceAsync(
        object? message,
        IReadOnlyCollection<MessageHeader>? headers = null,
        CancellationToken cancellationToken = default) =>
        ProduceAsync(OutboundEnvelopeFactory.CreateEnvelope(message, headers, EndpointConfiguration, this), cancellationToken);

    /// <inheritdoc cref="IProducer.ProduceAsync(IOutboundEnvelope,CancellationToken)" />
    public async ValueTask<IBrokerMessageIdentifier?> ProduceAsync(IOutboundEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            ProducerPipelineContext context = new(
                envelope,
                this,
                _behaviors,
                static async (finalContext, finalCancellationToken) =>
                {
                    ((RawOutboundEnvelope)finalContext.Envelope).BrokerMessageIdentifier =
                        finalContext.BrokerMessageIdentifier =
                            await ((Producer)finalContext.Producer).ProduceCoreAsync(finalContext.Envelope, finalCancellationToken).ConfigureAwait(false);

                    finalContext.ServiceProvider.GetRequiredService<IProducerLogger<IProducer>>().LogProduced(finalContext.Envelope);
                },
                _serviceProvider);

            await ExecutePipelineAsync(context, cancellationToken).ConfigureAwait(false);

            return context.BrokerMessageIdentifier;
        }
        catch (Exception ex)
        {
            _logger.LogProduceError(envelope, ex);
            throw;
        }
    }

    /// <inheritdoc cref="IProducer.RawProduceAsync(byte[],IReadOnlyCollection{MessageHeader}?,CancellationToken)" />
    public async ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(
        byte[]? messageContent,
        IReadOnlyCollection<MessageHeader>? headers = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IBrokerMessageIdentifier? brokerMessageIdentifier = await ProduceCoreAsync(
                new OutboundEnvelope(messageContent, headers, EndpointConfiguration, this),
                cancellationToken).ConfigureAwait(false);

            _logger.LogProduced(EndpointConfiguration, headers, brokerMessageIdentifier);

            return brokerMessageIdentifier;
        }
        catch (Exception ex)
        {
            _logger.LogProduceError(EndpointConfiguration, headers, ex);
            throw;
        }
    }

    /// <inheritdoc cref="IProducer.RawProduceAsync(Stream?,IReadOnlyCollection{MessageHeader}?,CancellationToken)" />
    public async ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IBrokerMessageIdentifier? brokerMessageIdentifier = await ProduceCoreAsync(
                new OutboundEnvelope(messageStream, headers, EndpointConfiguration, this),
                cancellationToken).ConfigureAwait(false);

            _logger.LogProduced(EndpointConfiguration, headers, brokerMessageIdentifier);

            return brokerMessageIdentifier;
        }
        catch (Exception ex)
        {
            _logger.LogProduceError(EndpointConfiguration, headers, ex);
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
    /// <typeparam name="TState">
    ///     The type of the state object that will be passed to the callbacks.
    /// </typeparam>
    /// <param name="envelope">
    ///     The envelope containing the message to be produced.
    /// </param>
    /// <param name="onSuccess">
    ///     The callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     The callback to be invoked when the produce fails.
    /// </param>
    /// <param name="state">
    ///     The state object that will be passed to the callbacks.
    /// </param>
    protected abstract void ProduceCore<TState>(
        IOutboundEnvelope envelope,
        Action<IBrokerMessageIdentifier?, TState> onSuccess,
        Action<Exception, TState> onError,
        TState state);

    /// <summary>
    ///     Publishes the specified message and returns its identifier.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message to be produced.
    /// </param>
    /// <param name="cancellationToken">
    ///     The cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The task result contains the
    ///     message identifier assigned by the broker (the Kafka offset or similar).
    /// </returns>
    protected abstract ValueTask<IBrokerMessageIdentifier?> ProduceCoreAsync(IOutboundEnvelope envelope, CancellationToken cancellationToken);

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

    private static ValueTask ExecutePipelineAsync(ProducerPipelineContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.CurrentStepIndex >= context.Pipeline.Count)
            return context.FinalAction(context, cancellationToken);

        return context.Pipeline[context.CurrentStepIndex].HandleAsync(
            context,
            static (nextContext, nextCancellationToken) =>
            {
                nextContext.CurrentStepIndex++;
                return ExecutePipelineAsync(nextContext, nextCancellationToken);
            },
            cancellationToken);
    }

    private void RawProduce(
        IOutboundEnvelope envelope,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        ProduceCore(
            envelope,
            static (identifier, state) =>
            {
                state.Logger.LogProduced(state.EndpointConfiguration, state.Headers, identifier);
                state.OnSuccess.Invoke(identifier);
            },
            static (exception, state) =>
            {
                state.Logger.LogProduceError(state.EndpointConfiguration, state.Headers, exception);
                state.OnError.Invoke(exception);
            },
            (OnSuccess: onSuccess, OnError: onError, EndpointConfiguration, envelope.Headers, Logger: _logger));

    private void RawProduce<TState>(
        IOutboundEnvelope envelope,
        Action<IBrokerMessageIdentifier?, TState> onSuccess,
        Action<Exception, TState> onError,
        TState state) =>
        ProduceCore(
            envelope,
            static (identifier, innerState) =>
            {
                innerState.Logger.LogProduced(innerState.EndpointConfiguration, innerState.Headers, identifier);
                innerState.OnSuccess.Invoke(identifier, innerState.State);
            },
            static (exception, innerState) =>
            {
                innerState.Logger.LogProduceError(innerState.EndpointConfiguration, innerState.Headers, exception);
                innerState.OnError.Invoke(exception, innerState.State);
            },
            (OnSuccess: onSuccess, OnError: onError, EndpointConfiguration, envelope.Headers, State: state, Logger: _logger));
}
