// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

/// <inheritdoc cref="IConsumer" />
/// <typeparam name="TBroker">
///     The type of the related <see cref="IBroker" /> implementation.
/// </typeparam>
/// <typeparam name="TConfiguration">
///     The type of the <see cref="ConsumerConfiguration" /> used by the consumer implementation.
/// </typeparam>
/// <typeparam name="TIdentifier">
///     The type of the <see cref="IBrokerMessageIdentifier" /> used by the consumer implementation.
/// </typeparam>
[SuppressMessage("", "CA1005", Justification = Justifications.NoWayToReduceTypeParameters)]
public abstract class Consumer<TBroker, TConfiguration, TIdentifier> : IConsumer, IDisposable
    where TBroker : class, IBroker
    where TConfiguration : ConsumerConfiguration
    where TIdentifier : class, IBrokerMessageIdentifier
{
    private static readonly TimeSpan ReconnectRetryDelay = TimeSpan.FromSeconds(5);

    private readonly IReadOnlyList<IConsumerBehavior> _behaviors;

    private readonly ISilverbackLogger<IConsumer> _logger;

    private readonly ConsumerStatusInfo _statusInfo = new();

    private readonly ConcurrentDictionary<IBrokerMessageIdentifier, int> _failedAttemptsDictionary = new();

    private readonly object _reconnectLock = new();

    private Task? _connectTask;

    private bool _isReconnecting;

    private bool _disposed;

    private CancellationTokenSource _disconnectCancellationTokenSource = new();

    private ISequenceStoreCollection? _sequenceStores;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Consumer{TBroker,TEndpoint,TIdentifier}" /> class.
    /// </summary>
    /// <param name="broker">
    ///     The <see cref="IBroker" /> that is instantiating the consumer.
    /// </param>
    /// <param name="configuration">
    ///     The endpoint to be consumed.
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
        TBroker broker,
        TConfiguration configuration,
        IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
        IServiceProvider serviceProvider,
        ISilverbackLogger<IConsumer> logger)
    {
        Broker = Check.NotNull(broker, nameof(broker));
        Configuration = Check.NotNull(configuration, nameof(configuration));

        _behaviors = Check.NotNull(behaviorsProvider, nameof(behaviorsProvider)).GetBehaviorsList();
        ServiceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        _logger = Check.NotNull(logger, nameof(logger));

        Configuration.Validate();
    }

    /// <inheritdoc cref="IBrokerConnectedObject.Id" />
    public InstanceIdentifier Id { get; } = new();

    /// <inheritdoc cref="IBrokerConnectedObject.Broker" />
    public TBroker Broker { get; }

    /// <inheritdoc cref="IConsumer.Configuration" />
    public TConfiguration Configuration { get; }

    /// <inheritdoc cref="IConsumer.StatusInfo" />
    public IConsumerStatusInfo StatusInfo => _statusInfo;

    /// <inheritdoc cref="IBrokerConnectedObject.IsConnecting" />
    public bool IsConnecting => _connectTask != null;

    /// <inheritdoc cref="IConsumer.SequenceStores" />
    public ISequenceStoreCollection SequenceStores => _sequenceStores ??= InitSequenceStore();

    /// <inheritdoc cref="IBrokerConnectedObject.IsConnected" />
    public bool IsConnected { get; private set; }

    /// <inheritdoc cref="IConsumer.IsConsuming" />
    public bool IsConsuming { get; protected set; }

    /// <inheritdoc cref="IBrokerConnectedObject.Broker" />
    IBroker IBrokerConnectedObject.Broker => Broker;

    /// <inheritdoc cref="IConsumer.Configuration" />
    ConsumerConfiguration IConsumer.Configuration => Configuration;

    /// <summary>
    ///     Gets the <see cref="IServiceProvider" /> to be used to resolve the required services.
    /// </summary>
    protected IServiceProvider ServiceProvider { get; }

    /// <summary>
    ///     Gets a value indicating whether the consumer is being disconnected.
    /// </summary>
    protected bool IsDisconnecting { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether the consumer is being stopped.
    /// </summary>
    protected bool IsStopping { get; private set; }

    /// <inheritdoc cref="IBrokerConnectedObject.ConnectAsync" />
    public async Task ConnectAsync() => await ConnectAndStartAsync().ConfigureAwait(false);

    /// <inheritdoc cref="IBrokerConnectedObject.DisconnectAsync" />
    public async Task DisconnectAsync()
    {
        _disconnectCancellationTokenSource.Cancel();
        await StopAndDisconnectAsync().ConfigureAwait(false);
    }

    /// <inheritdoc cref="IBrokerConnectedObject.DisconnectAsync" />
    [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
    public async Task TriggerReconnectAsync()
    {
        lock (_reconnectLock)
        {
            if (_isReconnecting || _disposed)
                return;

            _isReconnecting = true;
        }

        _logger.LogConsumerLowLevelTrace(this, "Triggering reconnect.");

        // Await stopping but disconnect/reconnect in a separate thread to avoid deadlocks
        await StopAsync().ConfigureAwait(false);

        Task.Run(
                async () =>
                {
                    // Clear trace context to avoid the consume loop to be tracked under the current
                    // message traceId
                    Activity.Current = null;

                    try
                    {
                        await StopAndDisconnectAsync().ConfigureAwait(false);

                        _disconnectCancellationTokenSource.Token.ThrowIfCancellationRequested();

                        await ConnectAndStartAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogErrorReconnectingConsumer(ReconnectRetryDelay, this, ex);

                        await Task.Delay(
                                ReconnectRetryDelay,
                                _disconnectCancellationTokenSource.Token)
                            .ConfigureAwait(false);

                        if (_disconnectCancellationTokenSource.Token.IsCancellationRequested)
                            return;

                        _isReconnecting = false;
                        TriggerReconnectAsync().FireAndForget();
                    }
                    finally
                    {
                        _isReconnecting = false;
                    }
                })
            .FireAndForget();
    }

    /// <inheritdoc cref="IConsumer.StartAsync" />
    public async Task StartAsync()
    {
        try
        {
            if (!IsConnected)
                throw new InvalidOperationException("The consumer is not connected.");

            if (IsConsuming)
                return;

            await _logger.ExecuteAndTraceConsumerActionAsync(
                    this,
                    StartCoreAsync,
                    "Starting consumer...",
                    "Consumer started.",
                    "Failed to start consumer.")
                .ConfigureAwait(false);

            IsConsuming = true;
        }
        catch (Exception ex)
        {
            _logger.LogConsumerStartError(this, ex);
            throw;
        }
    }

    /// <inheritdoc cref="IConsumer.StopAsync" />
    public async Task StopAsync()
    {
        try
        {
            if (!IsConsuming || IsStopping)
                return;

            IsStopping = true;

            await _logger.ExecuteAndTraceConsumerActionAsync(
                    this,
                    StopCoreAsync,
                    "Stopping consumer...",
                    "Consumer stopped.",
                    "Failed to stop consumer.")
                .ConfigureAwait(false);

            IsConsuming = false;
        }
        catch (Exception ex)
        {
            _logger.LogConsumerStopError(this, ex);
            throw;
        }
        finally
        {
            IsStopping = false;
        }
    }

    /// <inheritdoc cref="IConsumer.CommitAsync(IBrokerMessageIdentifier)" />
    public Task CommitAsync(IBrokerMessageIdentifier brokerMessageIdentifier)
    {
        Check.NotNull(brokerMessageIdentifier, nameof(brokerMessageIdentifier));

        return CommitAsync(new[] { brokerMessageIdentifier });
    }

    /// <inheritdoc cref="IConsumer.CommitAsync(IReadOnlyCollection{IBrokerMessageIdentifier})" />
    public async Task CommitAsync(IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers)
    {
        Check.NotNull(brokerMessageIdentifiers, nameof(brokerMessageIdentifiers));

        try
        {
            await CommitCoreAsync(brokerMessageIdentifiers.Cast<TIdentifier>().ToList()).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogConsumerCommitError(this, brokerMessageIdentifiers, ex);
            throw;
        }

        if (_failedAttemptsDictionary.IsEmpty)
            return;

        // TODO: Is this the most efficient way to remove a bunch of items?
        foreach (IBrokerMessageIdentifier? messageIdentifier in brokerMessageIdentifiers)
        {
            _failedAttemptsDictionary.TryRemove(messageIdentifier, out _);
        }
    }

    /// <inheritdoc cref="IConsumer.RollbackAsync(IBrokerMessageIdentifier)" />
    public Task RollbackAsync(IBrokerMessageIdentifier brokerMessageIdentifier)
    {
        Check.NotNull(brokerMessageIdentifier, nameof(brokerMessageIdentifier));

        return RollbackAsync(new[] { brokerMessageIdentifier });
    }

    /// <inheritdoc cref="IConsumer.RollbackAsync(IReadOnlyCollection{IBrokerMessageIdentifier})" />
    public Task RollbackAsync(IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers)
    {
        Check.NotNull(brokerMessageIdentifiers, nameof(brokerMessageIdentifiers));

        try
        {
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
    ///     Returns a newly initialized <see cref="ISequenceStoreCollection" /> to be used in the scope of this consumer.
    /// </summary>
    /// <returns>
    ///     An <see cref="ISequenceStoreCollection" /> instance.
    /// </returns>
    protected virtual ISequenceStoreCollection InitSequenceStore() =>
        new DefaultSequenceStoreCollection(ServiceProvider);

    /// <summary>
    ///     Connects to the message broker.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    protected abstract Task ConnectCoreAsync();

    /// <summary>
    ///     Disconnects from the message broker.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    protected abstract Task DisconnectCoreAsync();

    /// <summary>
    ///     Starts consuming. Called to resume consuming after <see cref="StopAsync" /> has been called.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    protected abstract Task StartCoreAsync();

    /// <summary>
    ///     Stops consuming while staying connected to the message broker.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    protected abstract Task StopCoreAsync();

    /// <summary>
    ///     Commits the specified messages sending the acknowledgement to the message broker.
    /// </summary>
    /// <param name="brokerMessageIdentifiers">
    ///     The identifiers of to message be committed.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    protected abstract Task CommitCoreAsync(IReadOnlyCollection<TIdentifier> brokerMessageIdentifiers);

    /// <summary>
    ///     If necessary notifies the message broker that the specified messages couldn't be processed
    ///     successfully, to ensure that they will be consumed again.
    /// </summary>
    /// <param name="brokerMessageIdentifiers">
    ///     The identifiers of to message be rolled back.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    protected abstract Task RollbackCoreAsync(IReadOnlyCollection<TIdentifier> brokerMessageIdentifiers);

    /// <summary>
    ///     Waits until the consuming is stopped.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    protected abstract Task WaitUntilConsumingStoppedCoreAsync();

    /// <summary>
    ///     Handles the consumed message invoking each <see cref="IConsumerBehavior" /> in the pipeline.
    /// </summary>
    /// <param name="message">
    ///     The body of the consumed message.
    /// </param>
    /// <param name="headers">
    ///     The headers of the consumed message.
    /// </param>
    /// <param name="actualEndpoint">
    ///     The endpoint from which the message was consumed.
    /// </param>
    /// <param name="brokerMessageIdentifier">
    ///     The identifier of the consumed message.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    [SuppressMessage("", "CA2000", Justification = "Context is disposed by the TransactionHandler")]
    protected virtual async Task HandleMessageAsync(
        byte[]? message,
        IReadOnlyCollection<MessageHeader> headers,
        ConsumerEndpoint actualEndpoint,
        IBrokerMessageIdentifier brokerMessageIdentifier)
    {
        RawInboundEnvelope envelope = new(
            message,
            headers,
            actualEndpoint,
            brokerMessageIdentifier);

        _statusInfo.RecordConsumedMessage(brokerMessageIdentifier);

        ConsumerPipelineContext consumerPipelineContext = new(
            envelope,
            this,
            SequenceStores.GetSequenceStore(brokerMessageIdentifier),
            ServiceProvider);

        await ExecutePipelineAsync(consumerPipelineContext).ConfigureAwait(false);
    }

    /// <summary>
    ///     Called when fully connected to transitions the consumer to <see cref="ConsumerStatus.Ready" />.
    /// </summary>
    protected void SetReadyStatus() => _statusInfo.SetReady();

    /// <summary>
    ///     Called when the connection is lost to transitions the consumer back to
    ///     <see cref="ConsumerStatus.Connected" />.
    /// </summary>
    protected void RevertReadyStatus()
    {
        if (_statusInfo.Status > ConsumerStatus.Connected)
            _statusInfo.SetConnected(true);
    }

    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
    ///     resources.
    /// </summary>
    /// <param name="disposing">
    ///     A value indicating whether the method has been called by the <c>Dispose</c> method and not from the
    ///     finalizer.
    /// </param>
    [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing || _disposed)
            return;

        _disposed = true;

        try
        {
            AsyncHelper.RunSynchronously(DisconnectAsync);

            if (_sequenceStores != null)
            {
                AsyncHelper.RunSynchronously(() => _sequenceStores.ForEachAsync(store => store.DisposeAsync().AsTask()));
            }

            _disconnectCancellationTokenSource.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogConsumerDisposingError(this, ex);
        }
    }

    private async Task ConnectAndStartAsync()
    {
        try
        {
            if (IsConnected)
                return;

            if (_disconnectCancellationTokenSource.IsCancellationRequested)
            {
                _disconnectCancellationTokenSource.Dispose();
                _disconnectCancellationTokenSource = new CancellationTokenSource();
            }

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

            _statusInfo.SetConnected();
            _logger.LogConsumerConnected(this);
        }
        catch (Exception ex)
        {
            _logger.LogConsumerConnectError(this, ex);
            throw;
        }

        await StartAsync().ConfigureAwait(false);
    }

    private async Task StopAndDisconnectAsync()
    {
        try
        {
            if (!IsConnected || IsDisconnecting)
                return;

            _logger.LogConsumerLowLevelTrace(
                this,
                "Disconnecting consumer...");

            IsDisconnecting = true;

            // Ensure that StopCore is called in any case to avoid deadlocks
            // (when the consumer loop is initialized but not started)
            if (IsConsuming && !IsStopping)
                await StopAsync().ConfigureAwait(false);
            else
                await StopCoreAsync().ConfigureAwait(false);

            await SequenceStores
                .AbortAllSequencesAsync(SequenceAbortReason.ConsumerAborted)
                .ConfigureAwait(false);

            await WaitUntilConsumingStoppedAsync().ConfigureAwait(false);
            await DisconnectCoreAsync().ConfigureAwait(false);

            IsConnected = false;

            _statusInfo.SetDisconnected();
            _logger.LogConsumerDisconnected(this);
        }
        catch (Exception ex)
        {
            _logger.LogConsumerDisconnectError(this, ex);
            throw;
        }
        finally
        {
            IsDisconnecting = false;
        }
    }

    private Task WaitUntilConsumingStoppedAsync() =>
        _logger.ExecuteAndTraceConsumerActionAsync(
            this,
            WaitUntilConsumingStoppedCoreAsync,
            "Waiting until consumer stops...",
            "Consumer stopped.");

    private Task ExecutePipelineAsync(ConsumerPipelineContext context, int stepIndex = 0)
    {
        if (_behaviors.Count == 0 || stepIndex >= _behaviors.Count)
            return Task.CompletedTask;

        return _behaviors[stepIndex].HandleAsync(
            context,
            nextContext => ExecutePipelineAsync(nextContext, stepIndex + 1));
    }
}
