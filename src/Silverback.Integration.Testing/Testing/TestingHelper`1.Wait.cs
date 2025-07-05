// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Producing.TransactionalOutbox;

namespace Silverback.Testing;

/// <content>
///     Implements the <c>Wait</c> methods.
/// </content>
[SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "Used for testing only")]
public abstract partial class TestingHelper
{
    private static readonly TimeSpan DefaultWaitTimeout = TimeSpan.FromSeconds(30);

    /// <inheritdoc cref="ITestingHelper.WaitUntilConnectedAsync(TimeSpan?)" />
    public ValueTask WaitUntilConnectedAsync(TimeSpan? timeout = null) =>
        WaitUntilConnectedAsync(true, timeout);

    /// <inheritdoc cref="ITestingHelper.WaitUntilConnectedAsync(bool,TimeSpan?)" />
    public ValueTask WaitUntilConnectedAsync(bool throwTimeoutException, TimeSpan? timeout = null)
    {
        using CancellationTokenSource cancellationTokenSource = new(timeout ?? DefaultWaitTimeout);
        return WaitUntilConnectedAsync(cancellationTokenSource.Token);
    }

    /// <inheritdoc cref="ITestingHelper.WaitUntilConnectedAsync(CancellationToken)" />
    public ValueTask WaitUntilConnectedAsync(CancellationToken cancellationToken) =>
        WaitUntilConnectedAsync(true, cancellationToken);

    /// <inheritdoc cref="ITestingHelper.WaitUntilConnectedAsync(bool,CancellationToken)" />
    public async ValueTask WaitUntilConnectedAsync(bool throwTimeoutException, CancellationToken cancellationToken)
    {
        if (_consumers == null || _consumers.Count == 0)
            return;

        try
        {
            while (_consumers.Any(consumer => consumer.StatusInfo.Status < ConsumerStatus.Connected))
            {
                cancellationToken.ThrowIfCancellationRequested();

                await Task.Delay(10, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException ex)
        {
            const string message = "Timeout elapsed before the consumers successfully established a connection";

            if (throwTimeoutException)
                throw new TimeoutException(message);

            _logger.LogWarning(ex, message);
        }
    }

    /// <inheritdoc cref="ITestingHelper.WaitUntilAllMessagesAreConsumedAsync(string[])" />
    public ValueTask WaitUntilAllMessagesAreConsumedAsync(params string[] endpointNames) =>
        WaitUntilAllMessagesAreConsumedAsync(true, null, endpointNames);

    /// <inheritdoc cref="ITestingHelper.WaitUntilAllMessagesAreConsumedAsync(bool,string[])" />
    public ValueTask WaitUntilAllMessagesAreConsumedAsync(TimeSpan? timeout, params string[] endpointNames) =>
        WaitUntilAllMessagesAreConsumedAsync(true, timeout, endpointNames);

    /// <inheritdoc cref="ITestingHelper.WaitUntilAllMessagesAreConsumedAsync(bool,string[])" />
    public ValueTask WaitUntilAllMessagesAreConsumedAsync(bool throwTimeoutException, params string[] endpointNames) =>
        WaitUntilAllMessagesAreConsumedAsync(throwTimeoutException, null, endpointNames);

    /// <inheritdoc cref="ITestingHelper.WaitUntilAllMessagesAreConsumedAsync(bool,TimeSpan?)" />
    public async ValueTask WaitUntilAllMessagesAreConsumedAsync(bool throwTimeoutException, TimeSpan? timeout = null)
    {
        using CancellationTokenSource cancellationTokenSource = new(timeout ?? DefaultWaitTimeout);
        await WaitUntilAllMessagesAreConsumedAsync(throwTimeoutException, cancellationTokenSource.Token).ConfigureAwait(false);
    }

    /// <inheritdoc cref="ITestingHelper.WaitUntilAllMessagesAreConsumedAsync(bool,TimeSpan?,string[])" />
    public async ValueTask WaitUntilAllMessagesAreConsumedAsync(bool throwTimeoutException, TimeSpan? timeout, params string[] endpointNames)
    {
        using CancellationTokenSource cancellationTokenSource = new(timeout ?? DefaultWaitTimeout);
        await WaitUntilAllMessagesAreConsumedAsync(throwTimeoutException, cancellationTokenSource.Token, endpointNames).ConfigureAwait(false);
    }

    /// <inheritdoc cref="ITestingHelper.WaitUntilAllMessagesAreConsumedAsync(CancellationToken,string[])" />
    public ValueTask WaitUntilAllMessagesAreConsumedAsync(CancellationToken cancellationToken, params string[] endpointNames) =>
        WaitUntilAllMessagesAreConsumedAsync(true, cancellationToken, endpointNames);

    /// <inheritdoc cref="ITestingHelper.WaitUntilAllMessagesAreConsumedAsync(bool,CancellationToken,string[])" />
    public async ValueTask WaitUntilAllMessagesAreConsumedAsync(
        bool throwTimeoutException,
        CancellationToken cancellationToken,
        params string[] endpointNames)
    {
        try
        {
            // Loop until the outbox is empty since the consumers may produce new messages
            do
            {
                await WaitUntilOutboxIsEmptyAsync(cancellationToken).ConfigureAwait(false);

                await WaitUntilAllMessagesAreConsumedCoreAsync(endpointNames ?? [], cancellationToken).ConfigureAwait(false);
            }
            while (!await IsOutboxEmptyAsync().ConfigureAwait(false));
        }
        catch (OperationCanceledException ex)
        {
            const string message = "Timeout elapsed before all messages could be consumed and processed";

            if (throwTimeoutException)
                throw new TimeoutException(message);

            _logger.LogWarning(ex, message);
        }
    }

    /// <inheritdoc cref="ITestingHelper.WaitUntilOutboxIsEmptyAsync(TimeSpan?)" />
    public async ValueTask WaitUntilOutboxIsEmptyAsync(TimeSpan? timeout = null)
    {
        using CancellationTokenSource cancellationTokenSource = new(timeout ?? DefaultWaitTimeout);

        await WaitUntilOutboxIsEmptyAsync(cancellationTokenSource.Token).ConfigureAwait(false);
    }

    /// <inheritdoc cref="ITestingHelper.WaitUntilOutboxIsEmptyAsync(CancellationToken)" />
    public async ValueTask WaitUntilOutboxIsEmptyAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (await IsOutboxEmptyAsync().ConfigureAwait(false))
                return;

            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc cref="ITestingHelper.IsOutboxEmptyAsync" />
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    public async ValueTask<bool> IsOutboxEmptyAsync()
    {
        try
        {
            foreach (OutboxWorkerService service in _serviceProvider.GetServices<IHostedService>().OfType<OutboxWorkerService>())
            {
                if (await service.OutboxWorker.GetLengthAsync().ConfigureAwait(false) > 0)
                    return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error occurred while checking whether the Outbox is empty");

            return false;
        }
    }

    /// <summary>
    ///     Returns a <see cref="ValueTask" /> that completes when all messages routed to the consumers have been processed and committed.
    /// </summary>
    /// <remarks>
    ///     This method works with the mocked brokers only.
    /// </remarks>
    /// <param name="endpointNames">
    ///     The names of the endpoints to wait for. If not specified, all endpoints are considered.
    /// </param>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> that completes when all messages have been processed.
    /// </returns>
    protected abstract Task WaitUntilAllMessagesAreConsumedCoreAsync(IReadOnlyCollection<string> endpointNames, CancellationToken cancellationToken);
}
