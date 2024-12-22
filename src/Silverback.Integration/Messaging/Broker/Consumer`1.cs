// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

/// <inheritdoc cref="IConsumer" />
/// <typeparam name="TIdentifier">
///     The type of the <see cref="IBrokerMessageIdentifier" /> used by the consumer implementation.
/// </typeparam>
public abstract class Consumer<TIdentifier> : IConsumer, IDisposable
    where TIdentifier : class, IBrokerMessageIdentifier
{
    private readonly IReadOnlyList<IConsumerBehavior> _behaviors;

    private readonly ISilverbackLogger<IConsumer> _logger;

    private readonly ConsumerStatusInfo _statusInfo = new();

    private readonly ConcurrentDictionary<IBrokerMessageIdentifier, int> _failedAttemptsDictionary = new();

    private readonly SemaphoreSlim _startStopSemaphore = new(1);

    private readonly object _reconnectLock = new();

    private CancellationTokenSource _processingCancellationTokenSource = new();

    private bool _isReconnecting;

    private bool _isDisposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Consumer{TIdentifier}" /> class.
    /// </summary>
    /// <param name="name">
    ///     The consumer name.
    /// </param>
    /// <param name="client">
    ///     The <see cref="IBrokerClient" />.
    /// </param>
    /// <param name="endpointsConfiguration">
    ///     The endpoints configuration.
    /// </param>
    /// <param name="behaviorsProvider">
    ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="ISilverbackLogger" />.
    /// </param>
    protected Consumer(
        string name,
        IBrokerClient client,
        IReadOnlyCollection<ConsumerEndpointConfiguration> endpointsConfiguration,
        IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
        IServiceProvider serviceProvider,
        ISilverbackLogger<IConsumer> logger)
    {
        Name = Check.NotNullOrEmpty(name, nameof(name));
        Client = Check.NotNull(client, nameof(client));
        EndpointsConfiguration = Check.NotNull(endpointsConfiguration, nameof(endpointsConfiguration));
        _behaviors = Check.NotNull(behaviorsProvider, nameof(behaviorsProvider)).GetBehaviorsList();
        ServiceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        _logger = Check.NotNull(logger, nameof(logger));

        Client.Initialized.AddHandler(OnClientConnectedAsync);
        Client.Disconnecting.AddHandler(OnClientDisconnectingAsync);
    }

    /// <inheritdoc cref="IConsumer.Name" />
    public string Name { get; }

    /// <inheritdoc cref="IConsumer.DisplayName" />
    /// <remarks>
    ///     The <see cref="DisplayName" /> is currently returning the <see cref="Name" /> but this might change in future implementations.
    /// </remarks>
    public string DisplayName => Name;

    /// <inheritdoc cref="IConsumer.Client" />
    public IBrokerClient Client { get; }

    /// <inheritdoc cref="IConsumer.EndpointsConfiguration" />
    public IReadOnlyCollection<ConsumerEndpointConfiguration> EndpointsConfiguration { get; }

    /// <inheritdoc cref="IConsumer.StatusInfo" />
    public IConsumerStatusInfo StatusInfo => _statusInfo;

    /// <summary>
    ///     Gets the <see cref="IServiceProvider" /> to be used to resolve the needed services.
    /// </summary>
    protected IServiceProvider ServiceProvider { get; }

    /// <summary>
    ///     Gets a value indicating whether the consumer is starting (or connecting to start).
    /// </summary>
    protected bool IsStarting { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether the consumer is started.
    /// </summary>
    protected bool IsStarted { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether the consumer is being stopped.
    /// </summary>
    protected bool IsStopping { get; private set; }

    /// <inheritdoc cref="IConsumer.TriggerReconnectAsync" />
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    public async ValueTask TriggerReconnectAsync()
    {
        lock (_reconnectLock)
        {
            if (_isReconnecting || _isDisposed)
                return;

            _isReconnecting = true;
        }

        _logger.LogConsumerLowLevelTrace(this, "Triggering reconnect.");

        // Await stopping but disconnect/reconnect in a separate thread to avoid deadlocks
        await StopAsync(false).ConfigureAwait(false);

        Task.Run(
                async () =>
                {
                    try
                    {
                        await Client.ReconnectAsync().ConfigureAwait(false);
                    }
                    finally
                    {
                        _isReconnecting = false;
                    }
                })
            .FireAndForget();
    }

    /// <inheritdoc cref="IConsumer.StartAsync" />
    public async ValueTask StartAsync()
    {
        await _startStopSemaphore.WaitAsync().ConfigureAwait(false);

        if (IsStarted)
        {
            _startStopSemaphore.Release();
            return;
        }

        if (Client.Status is not (ClientStatus.Initialized or ClientStatus.Initializing))
        {
            _startStopSemaphore.Release();
            throw new InvalidOperationException("The underlying client is not connected.");
        }

        IsStarting = true;

        _logger.LogConsumerLowLevelTrace(this, "Starting consumer...");

        try
        {
            if (_processingCancellationTokenSource.IsCancellationRequested)
            {
                _processingCancellationTokenSource.Dispose();
                _processingCancellationTokenSource = new CancellationTokenSource();
            }

            await StartCoreAsync().ConfigureAwait(false);

            IsStarted = true;

            _logger.LogConsumerLowLevelTrace(this, "Consumer started.");
        }
        catch (Exception ex)
        {
            _logger.LogConsumerStartError(this, ex);
            throw;
        }
        finally
        {
            IsStarting = false;
            _startStopSemaphore.Release();
        }
    }

    /// <inheritdoc cref="IConsumer.StopAsync" />
    public async ValueTask StopAsync(bool waitUntilStopped = true)
    {
        await _startStopSemaphore.WaitAsync().ConfigureAwait(false);

        if (!IsStarted)
        {
            _startStopSemaphore.Release();
            return;
        }

        IsStopping = true;

        _logger.LogConsumerLowLevelTrace(this, "Stopping consumer...");

        try
        {
#if NETSTANDARD
            _processingCancellationTokenSource.Cancel();
#else
            await _processingCancellationTokenSource.CancelAsync().ConfigureAwait(false);
#endif

            await StopCoreAsync().ConfigureAwait(false);

            if (waitUntilStopped)
                await WaitUntilConsumingStoppedAsync().ConfigureAwait(false);

            IsStarted = false;
        }
        catch (Exception ex)
        {
            _logger.LogConsumerStopError(this, ex);
            throw;
        }
        finally
        {
            IsStopping = false;
            _startStopSemaphore.Release();
        }
    }

    /// <inheritdoc cref="IConsumer.CommitAsync(IBrokerMessageIdentifier)" />
    public ValueTask CommitAsync(IBrokerMessageIdentifier brokerMessageIdentifier)
    {
        Check.NotNull(brokerMessageIdentifier, nameof(brokerMessageIdentifier));

        return CommitAsync([brokerMessageIdentifier]);
    }

    /// <inheritdoc cref="IConsumer.CommitAsync(IReadOnlyCollection{IBrokerMessageIdentifier})" />
    public async ValueTask CommitAsync(IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers)
    {
        Check.NotNull(brokerMessageIdentifiers, nameof(brokerMessageIdentifiers));

        try
        {
            await CommitCoreAsync(brokerMessageIdentifiers.Cast<TIdentifier>().ToList()).ConfigureAwait(false);

            if (_failedAttemptsDictionary.IsEmpty)
                return;

            foreach (IBrokerMessageIdentifier? messageIdentifier in brokerMessageIdentifiers)
            {
                _failedAttemptsDictionary.TryRemove(messageIdentifier, out _);
            }
        }
        catch (Exception ex)
        {
            _logger.LogConsumerCommitError(this, brokerMessageIdentifiers, ex);
            throw;
        }
    }

    /// <inheritdoc cref="IConsumer.RollbackAsync(IBrokerMessageIdentifier)" />
    public ValueTask RollbackAsync(IBrokerMessageIdentifier brokerMessageIdentifier)
    {
        Check.NotNull(brokerMessageIdentifier, nameof(brokerMessageIdentifier));

        return RollbackAsync([brokerMessageIdentifier]);
    }

    /// <inheritdoc cref="IConsumer.RollbackAsync(IReadOnlyCollection{IBrokerMessageIdentifier})" />
    public ValueTask RollbackAsync(IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers)
    {
        try
        {
            Check.NotNull(brokerMessageIdentifiers, nameof(brokerMessageIdentifiers));

            return RollbackCoreAsync(brokerMessageIdentifiers.Cast<TIdentifier>().ToList());
        }
        catch (Exception ex)
        {
            _logger.LogConsumerRollbackError(this, brokerMessageIdentifiers, ex);
            throw;
        }
    }

    /// <inheritdoc cref="IConsumer.IncrementFailedAttempts" />
    public int IncrementFailedAttempts(IRawInboundEnvelope envelope)
    {
        Check.NotNull(envelope, nameof(envelope));

        return _failedAttemptsDictionary.AddOrUpdate(
            envelope.BrokerMessageIdentifier,
            _ => envelope.Headers.GetValueOrDefault<int>(DefaultMessageHeaders.FailedAttempts) + 1,
            (_, count) => count + 1);
    }

    /// <inheritdoc cref="IDisposable.Dispose" />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Starts consuming. Called to resume consuming after <see cref="StopAsync" /> has been called.
    /// </summary>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    protected abstract ValueTask StartCoreAsync();

    /// <summary>
    ///     Stops consuming while staying connected to the message broker.
    /// </summary>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    protected abstract ValueTask StopCoreAsync();

    /// <summary>
    ///     Commits the specified messages sending the acknowledgement to the message broker.
    /// </summary>
    /// <param name="brokerMessageIdentifiers">
    ///     The identifiers of to message be committed.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    protected abstract ValueTask CommitCoreAsync(IReadOnlyCollection<TIdentifier> brokerMessageIdentifiers);

    /// <summary>
    ///     If necessary notifies the message broker that the specified messages couldn't be processed
    ///     successfully, to ensure that they will be consumed again.
    /// </summary>
    /// <param name="brokerMessageIdentifiers">
    ///     The identifiers of to message be rolled back.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    protected abstract ValueTask RollbackCoreAsync(IReadOnlyCollection<TIdentifier> brokerMessageIdentifiers);

    /// <summary>
    ///     Waits until the consuming is stopped.
    /// </summary>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    protected abstract ValueTask WaitUntilConsumingStoppedCoreAsync();

    /// <summary>
    ///     Handles the consumed message invoking each <see cref="IConsumerBehavior" /> in the pipeline.
    /// </summary>
    /// <param name="message">
    ///     The body of the consumed message.
    /// </param>
    /// <param name="headers">
    ///     The headers of the consumed message.
    /// </param>
    /// <param name="endpoint">
    ///     The endpoint from which the message was consumed.
    /// </param>
    /// <param name="brokerMessageIdentifier">
    ///     The identifier of the consumed message.
    /// </param>
    /// <param name="sequenceStore">
    ///     The <see cref="ISequenceStore" /> to be used.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Context is disposed by the TransactionHandler")]
    protected virtual async ValueTask HandleMessageAsync(
        byte[]? message,
        IReadOnlyCollection<MessageHeader> headers,
        ConsumerEndpoint endpoint,
        IBrokerMessageIdentifier brokerMessageIdentifier,
        ISequenceStore sequenceStore)
    {
        RawInboundEnvelope envelope = new(message, headers, endpoint, this, brokerMessageIdentifier);
        ConsumerPipelineContext context = new(envelope, this, sequenceStore, _behaviors, ServiceProvider);

        _statusInfo.RecordConsumedMessage(brokerMessageIdentifier);

        await ExecutePipelineAsync(context, _processingCancellationTokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    ///     Called when fully connected to transitions the consumer to <see cref="ConsumerStatus.Connected" />.
    /// </summary>
    protected void SetConnectedStatus() => _statusInfo.SetConnected();

    /// <summary>
    ///     Called when the connection is lost to transitions the consumer back to <see cref="ConsumerStatus.Started" />.
    /// </summary>
    protected void RevertConnectedStatus()
    {
        if (_statusInfo.Status > ConsumerStatus.Started)
            _statusInfo.SetStarted(true);
    }

    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <param name="disposing">
    ///     A value indicating whether the method has been called by the <c>Dispose</c> method and not from the finalizer.
    /// </param>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing || _isDisposed)
            return;

        StopAsync(false).SafeWait();

        Client.Initialized.RemoveHandler(OnClientConnectedAsync);
        Client.Disconnecting.RemoveHandler(OnClientDisconnectingAsync);

        _startStopSemaphore.Dispose();
        _processingCancellationTokenSource.Dispose();

        _isDisposed = true;
    }

    /// <summary>
    ///     Gets a value indicating whether the consumer is started (or will start) and is not being stopped.
    /// </summary>
    /// <returns>
    ///     A value indicating whether the consumer is started and not being stopped.
    /// </returns>
    protected bool IsStartedAndNotStopping() =>
        Client.Status is ClientStatus.Initialized or ClientStatus.Initializing && (IsStarting || IsStarted) && !IsStopping;

    private static ValueTask ExecutePipelineAsync(ConsumerPipelineContext context, CancellationToken cancellationToken)
    {
        if (context.CurrentStepIndex >= context.Pipeline.Count)
            return ValueTaskFactory.CompletedTask;

        return context.Pipeline[context.CurrentStepIndex].HandleAsync(
            context,
            static (nextContext, nextCancellationToken) =>
            {
                nextContext.CurrentStepIndex++;
                return ExecutePipelineAsync(nextContext, nextCancellationToken);
            },
            cancellationToken);
    }

    private ValueTask OnClientConnectedAsync(BrokerClient client)
    {
        _statusInfo.SetStarted();
        return default;
    }

    private async ValueTask OnClientDisconnectingAsync(BrokerClient client)
    {
        if (IsStarted && !IsStopping)
            await StopAsync(false).ConfigureAwait(false);
        else
            await StopCoreAsync().ConfigureAwait(false);

        await WaitUntilConsumingStoppedAsync().ConfigureAwait(false);
    }

    private async ValueTask WaitUntilConsumingStoppedAsync()
    {
        _logger.LogConsumerLowLevelTrace(this, "Waiting until consumer stops...");

        try
        {
            await WaitUntilConsumingStoppedCoreAsync().ConfigureAwait(false);

            _statusInfo.SetStopped();
        }
        finally
        {
            _logger.LogConsumerLowLevelTrace(this, "Consumer stopped.");
        }
    }
}
