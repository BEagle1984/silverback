// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Routing;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

/// <inheritdoc cref="IProducer" />
public abstract class Producer : IProducer, IDisposable
{
    private readonly IReadOnlyList<IProducerBehavior> _behaviors;

    private readonly IServiceProvider _serviceProvider;

    private readonly ISilverbackLogger<IProducer> _logger;

    private readonly IBrokerActivityEnricher _activityEnricher;

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
    ///     The <see cref="IServiceProvider" /> to be used to resolve the necessary services.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="ISilverbackLogger{TCategoryName}" />.
    /// </param>
    protected Producer(
        string name,
        IBrokerClient client,
        ProducerEndpointConfiguration endpointConfiguration,
        IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
        IServiceProvider serviceProvider,
        ISilverbackLogger<IProducer> logger)
    {
        Name = Check.NotNullOrEmpty(name);
        EndpointConfiguration = Check.NotNull(endpointConfiguration);
        Client = Check.NotNull(client);
        _behaviors = Check.NotNull(behaviorsProvider).GetBehaviorsList();
        _serviceProvider = Check.NotNull(serviceProvider);
        _logger = Check.NotNull(logger);
        _activityEnricher = serviceProvider.GetService<IActivityEnricherFactory>()?.GetEnricher(endpointConfiguration, serviceProvider) ??
                            NullBrokerActivityEnricher.Instance;
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
    [SuppressMessage("Usage", "VSTHRD103:Call async methods when in an async method", Justification = "Produce is meant to be synchronous")]
    public IBrokerMessageIdentifier? Produce(IOutboundEnvelope envelope)
    {
        Check.NotNull(envelope);

        try
        {
            ProducerPipelineContext context = new(
                envelope,
                this,
                _behaviors,
                static (finalContext, _) =>
                {
                    Producer producer = (Producer)finalContext.Producer;

                    try
                    {
                        ((OutboundEnvelope)finalContext.Envelope).BrokerMessageIdentifier =
                            finalContext.BrokerMessageIdentifier =
                                producer.ProduceCore(finalContext.Envelope);
                    }
                    catch
                    {
                        producer.EnrichActivity(finalContext.Activity, finalContext.Envelope);
                        throw;
                    }

                    producer.LogProduced(finalContext);

                    return ValueTask.CompletedTask;
                },
                envelope.Context?.ServiceProvider ?? _serviceProvider);

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
        Check.NotNull(envelope);

        ProducerPipelineContext<TState> context = new(
            envelope,
            this,
            _behaviors,
            static (finalContext, _) =>
            {
                // The callbacks are usually invoked from another thread, after the pipeline returned. The activity is still running, since
                // the tracker stops it only once all the messages have been acknowledged.
                static void OnSuccess(IBrokerMessageIdentifier? identifier, ProducerPipelineContext finalContext)
                {
                    ((OutboundEnvelope)finalContext.Envelope).BrokerMessageIdentifier = finalContext.BrokerMessageIdentifier = identifier;
                    ProduceCallbackTracker<TState> callbackTracker = ((ProducerPipelineContext<TState>)finalContext).CallbackTracker!;

                    lock (callbackTracker.CallbacksLock)
                    {
                        ((Producer)finalContext.Producer).LogProduced(finalContext);
                    }

                    callbackTracker.OnProduced();
                }

                static void OnError(Exception exception, ProducerPipelineContext finalContext)
                {
                    ProduceCallbackTracker<TState> callbackTracker = ((ProducerPipelineContext<TState>)finalContext).CallbackTracker!;

                    lock (callbackTracker.CallbacksLock)
                    {
                        ((Producer)finalContext.Producer).LogProduceError(finalContext, exception);
                    }

                    callbackTracker.OnProduceFailed(exception);
                }

                // The final action is invoked once per produced message, which might be more than one (e.g. one per chunk)
                ProduceCallbackTracker<TState> callbackTracker = ((ProducerPipelineContext<TState>)finalContext).CallbackTracker!;
                callbackTracker.OnProducing();

                try
                {
                    ((Producer)finalContext.Producer).ProduceCore(
                        finalContext.Envelope,
                        OnSuccess,
                        OnError,
                        finalContext);
                }
                catch
                {
                    // Nothing was produced and no callback will be invoked, but a behavior might still handle the exception (e.g. retry)
                    callbackTracker.OnProducingFailed();

                    lock (callbackTracker.CallbacksLock)
                    {
                        ((Producer)finalContext.Producer).EnrichActivity(finalContext.Activity, finalContext.Envelope);
                    }

                    throw;
                }

                return ValueTask.CompletedTask;
            },
            envelope.Context?.ServiceProvider ?? _serviceProvider);

        context.CallbackTracker = new ProduceCallbackTracker<TState>(context, onSuccess, onError, state);

        try
        {
            ExecutePipelineAsync(context, CancellationToken.None).SafeWait();
        }
        catch (Exception ex)
        {
            // The tracker is not released, so the callbacks are never invoked by the messages produced before the exception
            _logger.LogProduceError(envelope, ex);
            throw;
        }

        // Invokes the callbacks right away if nothing was produced (e.g. filtered out) or everything was already acknowledged
        context.CallbackTracker.OnPipelineCompleted();
    }

    /// <inheritdoc cref="IProducer.RawProduce(byte[],IReadOnlyCollection{MessageHeader}?)" />
    public IBrokerMessageIdentifier? RawProduce(byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers = null) =>
        RawProduce(new OutboundEnvelope(messageContent, headers, EndpointConfiguration, this));

    /// <inheritdoc cref="IProducer.RawProduce(Stream?,IReadOnlyCollection{MessageHeader}?)" />
    public IBrokerMessageIdentifier? RawProduce(Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers = null) =>
        RawProduce(new OutboundEnvelope(messageStream, headers, EndpointConfiguration, this));

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
        Check.NotNull(envelope);

        try
        {
            ProducerPipelineContext context = new(
                envelope,
                this,
                _behaviors,
                static async (finalContext, finalCancellationToken) =>
                {
                    Producer producer = (Producer)finalContext.Producer;

                    try
                    {
                        ((OutboundEnvelope)finalContext.Envelope).BrokerMessageIdentifier =
                            finalContext.BrokerMessageIdentifier =
                                await producer.ProduceCoreAsync(finalContext.Envelope, finalCancellationToken).ConfigureAwait(false);
                    }
                    catch
                    {
                        producer.EnrichActivity(finalContext.Activity, finalContext.Envelope);
                        throw;
                    }

                    producer.LogProduced(finalContext);
                },
                envelope.Context?.ServiceProvider ?? _serviceProvider);

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
    public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(
        byte[]? messageContent,
        IReadOnlyCollection<MessageHeader>? headers = null,
        CancellationToken cancellationToken = default) =>
        RawProduceAsync(new OutboundEnvelope(messageContent, headers, EndpointConfiguration, this), cancellationToken);

    /// <inheritdoc cref="IProducer.RawProduceAsync(Stream?,IReadOnlyCollection{MessageHeader}?,CancellationToken)" />
    public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers = null,
        CancellationToken cancellationToken = default) =>
        RawProduceAsync(new OutboundEnvelope(messageStream, headers, EndpointConfiguration, this), cancellationToken);

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
    ///     In this implementation, the message is synchronously enqueued but produced asynchronously. The callbacks
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

    private IBrokerMessageIdentifier? RawProduce(OutboundEnvelope envelope)
    {
        Activity? activity = Diagnostics.ActivitySources.GetCurrentProduceActivity();

        try
        {
            envelope.BrokerMessageIdentifier = ProduceCore(envelope);
        }
        catch (Exception ex)
        {
            LogProduceError(_logger, envelope, activity, ex);
            throw;
        }

        LogProduced(_logger, envelope, activity);
        return envelope.BrokerMessageIdentifier;
    }

    private void RawProduce(
        OutboundEnvelope envelope,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        RawProduce(
            envelope,
            static (identifier, state) => state.OnSuccess.Invoke(identifier),
            static (exception, state) => state.OnError.Invoke(exception),
            (OnSuccess: onSuccess, OnError: onError));

    // The callbacks are usually invoked from another thread, so the produce activity (e.g. started by the outbox worker) is captured here.
    // It is stopped by its owner, in the callbacks, after the outcome was logged.
    private void RawProduce<TState>(
        OutboundEnvelope envelope,
        Action<IBrokerMessageIdentifier?, TState> onSuccess,
        Action<Exception, TState> onError,
        TState state) =>
        ProduceCore(
            envelope,
            static (identifier, innerState) =>
            {
                innerState.Envelope.BrokerMessageIdentifier = identifier;
                innerState.Producer.LogProduced(innerState.Producer._logger, innerState.Envelope, innerState.Activity);
                innerState.OnSuccess.Invoke(identifier, innerState.State);
            },
            static (exception, innerState) =>
            {
                innerState.Producer.LogProduceError(innerState.Producer._logger, innerState.Envelope, innerState.Activity, exception);
                innerState.OnError.Invoke(exception, innerState.State);
            },
            (OnSuccess: onSuccess,
                OnError: onError,
                Envelope: envelope,
                State: state,
                Activity: Diagnostics.ActivitySources.GetCurrentProduceActivity(),
                Producer: this));

    private async ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(OutboundEnvelope envelope, CancellationToken cancellationToken)
    {
        Activity? activity = Diagnostics.ActivitySources.GetCurrentProduceActivity();

        try
        {
            envelope.BrokerMessageIdentifier = await ProduceCoreAsync(envelope, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogProduceError(_logger, envelope, activity, ex);
            throw;
        }

        LogProduced(_logger, envelope, activity);
        return envelope.BrokerMessageIdentifier;
    }

    private void LogProduced(ProducerPipelineContext context) =>
        LogProduced(context.ServiceProvider.GetRequiredService<ISilverbackLogger<IProducer>>(), context.Envelope, context.Activity);

    // The activity is enriched right before logging and set as current while logging, so that its tags (e.g. the Kafka key and offset)
    // are available to the logging scope even when the outcome is logged from the thread delivering the broker acknowledgment (the same
    // applies to LogProduceError)
    private void LogProduced(ISilverbackLogger logger, IOutboundEnvelope envelope, Activity? activity)
    {
        EnrichActivity(activity, envelope);

        using CurrentActivityScope scope = CurrentActivityScope.Enter(activity);
        logger.LogProduced(envelope);
    }

    private void LogProduceError(ProducerPipelineContext context, Exception exception) =>
        LogProduceError(context.ServiceProvider.GetRequiredService<ISilverbackLogger<IProducer>>(), context.Envelope, context.Activity, exception);

    private void LogProduceError(ISilverbackLogger logger, IOutboundEnvelope envelope, Activity? activity, Exception exception)
    {
        EnrichActivity(activity, envelope);

        using CurrentActivityScope scope = CurrentActivityScope.Enter(activity);
        logger.LogProduceError(envelope, exception);
    }

    private void EnrichActivity(Activity? activity, IOutboundEnvelope envelope)
    {
        if (activity != null)
            _activityEnricher.EnrichOutboundActivity(activity, envelope);
    }
}
