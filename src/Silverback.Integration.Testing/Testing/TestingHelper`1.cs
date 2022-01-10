// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Util;

namespace Silverback.Testing;

/// <inheritdoc cref="ITestingHelper{TBroker}" />
public abstract class TestingHelper<TBroker> : ITestingHelper<TBroker>
    where TBroker : IBroker
{
    private readonly ILogger _logger;

    private readonly IServiceProvider _serviceProvider;

    private readonly TBroker _broker;

    private readonly IIntegrationSpy? _integrationSpy;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TestingHelper{TBroker}" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" />.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="ILogger{TCategoryName}" />.
    /// </param>
    protected TestingHelper(IServiceProvider serviceProvider, ILogger<TestingHelper<TBroker>> logger)
    {
        _logger = logger;
        _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));

        _broker = serviceProvider.GetRequiredService<TBroker>();
        _integrationSpy = serviceProvider.GetService<IIntegrationSpy>();
    }

    /// <inheritdoc cref="ITestingHelper{TBroker}.Broker" />
    public TBroker Broker => _broker ?? throw new InvalidOperationException($"No broker of type {typeof(TBroker).Name} could be resolved.");

    /// <inheritdoc cref="ITestingHelper{TBroker}.Spy" />
    public IIntegrationSpy Spy => _integrationSpy ?? throw new InvalidOperationException(
        "The IIntegrationSpy couldn't be resolved. " +
        "Register it calling AddIntegrationSpy or AddIntegrationSpyAndSubscriber.");

    /// <inheritdoc cref="ITestingHelper{TBroker}.WaitUntilConnectedAsync(TimeSpan?)" />
    public Task WaitUntilConnectedAsync(TimeSpan? timeout = null) =>
        WaitUntilConnectedAsync(true, timeout);

    /// <inheritdoc cref="ITestingHelper{TBroker}.WaitUntilConnectedAsync(bool,TimeSpan?)" />
    public Task WaitUntilConnectedAsync(bool throwTimeoutException, TimeSpan? timeout = null)
    {
        using CancellationTokenSource cancellationTokenSource = new(timeout ?? TimeSpan.FromSeconds(30));
        return WaitUntilConnectedAsync(cancellationTokenSource.Token);
    }

    /// <inheritdoc cref="ITestingHelper{TBroker}.WaitUntilConnectedAsync(CancellationToken)" />
    public Task WaitUntilConnectedAsync(CancellationToken cancellationToken) =>
        WaitUntilConnectedAsync(true, cancellationToken);

    /// <inheritdoc cref="ITestingHelper{TBroker}.WaitUntilConnectedAsync(bool,CancellationToken)" />
    public async Task WaitUntilConnectedAsync(bool throwTimeoutException, CancellationToken cancellationToken)
    {
        try
        {
            while (!_broker.IsConnected || _broker.Consumers.Any(consumer => consumer.StatusInfo.Status < ConsumerStatus.Ready))
            {
                cancellationToken.ThrowIfCancellationRequested();

                await Task.Delay(10, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            const string message = "Timeout elapsed before the consumers successfully established a connection";

            if (throwTimeoutException)
                throw new TimeoutException(message);

            _logger.LogWarning(message);
        }
    }

    /// <inheritdoc cref="ITestingHelper{TBroker}.WaitUntilAllMessagesAreConsumedAsync(TimeSpan?)" />
    public Task WaitUntilAllMessagesAreConsumedAsync(TimeSpan? timeout = null) =>
        WaitUntilAllMessagesAreConsumedAsync(true, timeout);

    /// <inheritdoc cref="ITestingHelper{TBroker}.WaitUntilAllMessagesAreConsumedAsync(bool,TimeSpan?)" />
    public Task WaitUntilAllMessagesAreConsumedAsync(bool throwTimeoutException, TimeSpan? timeout = null)
    {
        using CancellationTokenSource cancellationTokenSource = new(timeout ?? TimeSpan.FromSeconds(30));
        return WaitUntilAllMessagesAreConsumedAsync(throwTimeoutException, CancellationToken.None);
    }

    /// <inheritdoc cref="ITestingHelper{TBroker}.WaitUntilAllMessagesAreConsumedAsync(CancellationToken)" />
    public Task WaitUntilAllMessagesAreConsumedAsync(CancellationToken cancellationToken) =>
        WaitUntilAllMessagesAreConsumedAsync(true, cancellationToken);

    /// <inheritdoc cref="ITestingHelper{TBroker}.WaitUntilAllMessagesAreConsumedAsync(bool,CancellationToken)" />
    public async Task WaitUntilAllMessagesAreConsumedAsync(
        bool throwTimeoutException,
        CancellationToken cancellationToken)
    {
        try
        {
            // Loop until the outbox is empty since the consumers may produce new messages
            do
            {
                await WaitUntilOutboxIsEmptyAsync(cancellationToken).ConfigureAwait(false);

                await WaitUntilAllMessagesAreConsumedCoreAsync(cancellationToken).ConfigureAwait(false);
            }
            while (!await IsOutboxEmptyAsync().ConfigureAwait(false));
        }
        catch (OperationCanceledException)
        {
            const string message = "Timeout elapsed before all messages could be consumed and processed";

            if (throwTimeoutException)
                throw new TimeoutException(message);

            _logger.LogWarning(message);
        }
    }

    /// <inheritdoc cref="ITestingHelper{TBroker}.WaitUntilOutboxIsEmptyAsync(TimeSpan?)" />
    public Task WaitUntilOutboxIsEmptyAsync(TimeSpan? timeout = null)
    {
        using CancellationTokenSource cancellationTokenSource = new(timeout ?? TimeSpan.FromSeconds(30));

        return WaitUntilOutboxIsEmptyAsync(cancellationTokenSource.Token);
    }

    /// <inheritdoc cref="ITestingHelper{TBroker}.WaitUntilOutboxIsEmptyAsync(CancellationToken)" />
    public async Task WaitUntilOutboxIsEmptyAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (await IsOutboxEmptyAsync().ConfigureAwait(false))
                return;

            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc cref="ITestingHelper{TBroker}.IsOutboxEmptyAsync" />
    [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
    public async Task<bool> IsOutboxEmptyAsync()
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

    /// <inheritdoc cref="ITestingHelper{TBroker}.WaitUntilAllMessagesAreConsumedAsync(CancellationToken)" />
    protected abstract Task WaitUntilAllMessagesAreConsumedCoreAsync(CancellationToken cancellationToken);
}
