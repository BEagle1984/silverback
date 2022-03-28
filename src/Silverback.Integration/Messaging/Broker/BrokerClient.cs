// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

/// <inheritdoc cref="IBrokerClient" />
public abstract class BrokerClient : IBrokerClient
{
    private readonly ISilverbackLogger _logger;

    private CancellationTokenSource _disconnectCancellationTokenSource = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="BrokerClient" /> class.
    /// </summary>
    /// <param name="name">
    ///     The client name.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="ISilverbackLogger" />.
    /// </param>
    protected BrokerClient(string name, ISilverbackLogger logger)
    {
        Name = Check.NotNullOrEmpty(name, nameof(name));
        _logger = Check.NotNull(logger, nameof(logger));
    }

    /// <summary>
    ///     Finalizes an instance of the <see cref="BrokerClient" /> class.
    /// </summary>
    ~BrokerClient() => Dispose(false);

    /// <inheritdoc cref="IBrokerClient.Name" />
    public string Name { get; }

    /// <inheritdoc cref="IBrokerClient.DisplayName" />
    public string DisplayName => Name;

    /// <inheritdoc cref="IBrokerClient.Initializing" />
    public AsyncEvent<BrokerClient> Initializing { get; } = new();

    /// <inheritdoc cref="IBrokerClient.Initialized" />
    public AsyncEvent<BrokerClient> Initialized { get; } = new();

    /// <inheritdoc cref="IBrokerClient.Disconnecting" />
    public AsyncEvent<BrokerClient> Disconnecting { get; } = new();

    /// <inheritdoc cref="IBrokerClient.Disconnected" />
    public AsyncEvent<BrokerClient> Disconnected { get; } = new();

    /// <inheritdoc cref="IBrokerClient.Status" />
    public ClientStatus Status { get; private set; }

    /// <inheritdoc cref="IDisposable.Dispose" />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="IAsyncDisposable.DisposeAsync" />
    public async ValueTask DisposeAsync()
    {
        await DisposeCoreAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="IBrokerClient.ConnectAsync" />
    public async ValueTask ConnectAsync()
    {
        if (Status != ClientStatus.Disconnected)
            return;

        _logger.LogBrokerClientInitializing(this);

        try
        {
            Status = ClientStatus.Initializing;
            await Initializing.InvokeAsync(this).ConfigureAwait(false);

            if (_disconnectCancellationTokenSource.IsCancellationRequested)
            {
                _disconnectCancellationTokenSource.Dispose();
                _disconnectCancellationTokenSource = new CancellationTokenSource();
            }

            await ConnectCoreAsync().ConfigureAwait(false);

            await Initialized.InvokeAsync(this).ConfigureAwait(false);
            Status = ClientStatus.Initialized;

            _logger.LogBrokerClientInitialized(this);
        }
        catch (Exception ex)
        {
            _logger.LogBrokerClientInitializeError(this, ex);

            Status = ClientStatus.Disconnected;
            throw;
        }
    }

    /// <inheritdoc cref="IBrokerClient.DisconnectAsync" />
    public ValueTask DisconnectAsync() => DisconnectAsync(false);

    /// <inheritdoc cref="IBrokerClient.ReconnectAsync" />
    public async ValueTask ReconnectAsync()
    {
        // Clear trace context to avoid the consume loop to be tracked under the current
        // message traceId
        Activity.Current = null;

        while (!await TryReconnectAsync().ConfigureAwait(false))
        {
            // Just keep looping until it reconnects
        }
    }

    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <param name="disposing">
    ///     A value indicating whether the method has been called by the <c>Dispose</c> method and not from the finalizer.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        AsyncHelper.RunSynchronously(DisconnectAsync);

        _disconnectCancellationTokenSource.Dispose();
    }

    /// <inheritdoc cref="IAsyncDisposable.DisposeAsync" />
    protected virtual async ValueTask DisposeCoreAsync()
    {
        await DisconnectAsync().ConfigureAwait(false);

        _disconnectCancellationTokenSource.Dispose();
    }

    /// <summary>
    ///     Called when <see cref="ConnectAsync" /> is called to initialize the client.
    /// </summary>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    protected abstract ValueTask ConnectCoreAsync();

    /// <summary>
    ///     Called when <see cref="DisconnectAsync()" /> is called to disconnect the client.
    /// </summary>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    protected abstract ValueTask DisconnectCoreAsync();

    private async ValueTask DisconnectAsync(bool isRestarting)
    {
        if (!isRestarting && !_disconnectCancellationTokenSource.IsCancellationRequested)
            _disconnectCancellationTokenSource.Cancel();

        if (Status != ClientStatus.Initialized)
            return;

        _logger.LogBrokerClientDisconnecting(this);

        try
        {
            Status = ClientStatus.Disconnecting;
            await Disconnecting.InvokeAsync(this).ConfigureAwait(false);

            await DisconnectCoreAsync().ConfigureAwait(false);

            await Disconnected.InvokeAsync(this).ConfigureAwait(false);
            Status = ClientStatus.Disconnected;

            _logger.LogBrokerClientDisconnected(this);
        }
        catch (Exception ex)
        {
            _logger.LogBrokerClientDisconnectError(this, ex);

            Status = ClientStatus.Initialized;
            throw;
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Logged")]
    private async ValueTask<bool> TryReconnectAsync()
    {
        try
        {
            await DisconnectAsync(true).ConfigureAwait(false);

            if (_disconnectCancellationTokenSource.Token.IsCancellationRequested)
                return true;

            await ConnectAsync().ConfigureAwait(false);

            return true;
        }
        catch (Exception ex)
        {
            if (_disconnectCancellationTokenSource.Token.IsCancellationRequested)
                return true;

            _logger.LogBrokerClientReconnectError(this, BrokerConstants.ConsumerReconnectRetryDelay, ex);

            await Task.Delay(BrokerConstants.ConsumerReconnectRetryDelay, _disconnectCancellationTokenSource.Token).ConfigureAwait(false);

            return _disconnectCancellationTokenSource.Token.IsCancellationRequested;
        }
    }
}
