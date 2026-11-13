// Copyright (c) 2026 Sergio Aquilini
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
using Silverback.Util;

namespace Silverback.Testing;

/// <inheritdoc cref="IOutboxTestingHelper" />
[SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "Used for testing only")]
internal sealed class OutboxTestingHelper : IOutboxTestingHelper
{
    private const int ClearBatchSize = 1000;

    private static readonly TimeSpan DefaultWaitTimeout = TimeSpan.FromSeconds(30);

    private readonly IServiceProvider _serviceProvider;

    private readonly ILogger _logger;

    public OutboxTestingHelper(IServiceProvider serviceProvider, ILogger logger)
    {
        _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        _logger = Check.NotNull(logger, nameof(logger));
    }

    public async ValueTask<IReadOnlyList<OutboxMessage>> GetMessagesAsync(OutboxSettings? outboxSettings = null)
    {
        List<OutboxMessage> messages = [];

        foreach (IOutboxReader reader in GetReaders(outboxSettings))
        {
            messages.AddRange(await ReadAsync(reader, int.MaxValue).ConfigureAwait(false));
        }

        return messages;
    }

    public async ValueTask<int> GetLengthAsync(OutboxSettings? outboxSettings = null)
    {
        int length = 0;

        foreach (IOutboxReader reader in GetReaders(outboxSettings))
        {
            length += await reader.GetLengthAsync().ConfigureAwait(false);
        }

        return length;
    }

    public async ValueTask<bool> IsEmptyAsync(OutboxSettings? outboxSettings = null) =>
        await GetLengthAsync(outboxSettings).ConfigureAwait(false) == 0;

    public async ValueTask WaitUntilEmptyAsync(TimeSpan? timeout = null, OutboxSettings? outboxSettings = null)
    {
        using CancellationTokenSource cancellationTokenSource = new(timeout ?? DefaultWaitTimeout);

        try
        {
            await WaitUntilEmptyAsync(cancellationTokenSource.Token, outboxSettings).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
            throw new TimeoutException("Timeout elapsed before the outbox was emptied");
        }
    }

    public async ValueTask WaitUntilEmptyAsync(CancellationToken cancellationToken, OutboxSettings? outboxSettings = null)
    {
        // Resolved up front, to throw right away if the specified outbox cannot be read (e.g. not configured)
        List<IOutboxReader>? readers = outboxSettings == null ? null : [.. GetReaders(outboxSettings)];

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await IsEmptyIgnoringErrorsAsync(readers).ConfigureAwait(false))
                return;

            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask ClearAsync(OutboxSettings? outboxSettings = null, CancellationToken cancellationToken = default)
    {
        foreach (IOutboxReader reader in GetReaders(outboxSettings))
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                List<OutboxMessage> messages = await ReadAsync(reader, ClearBatchSize).ConfigureAwait(false);

                if (messages.Count == 0)
                    break;

                await reader.AcknowledgeAsync(messages).ConfigureAwait(false);
            }
        }
    }

    public async ValueTask ProcessAsync(CancellationToken cancellationToken = default)
    {
        foreach (OutboxWorkerService service in GetWorkerServices(null))
        {
            await service.ProcessUntilEmptyAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask ProcessAsync(OutboxSettings outboxSettings, CancellationToken cancellationToken = default)
    {
        foreach (OutboxWorkerService service in GetWorkerServices(Check.NotNull(outboxSettings, nameof(outboxSettings))))
        {
            await service.ProcessUntilEmptyAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask PauseWorkerAsync(CancellationToken cancellationToken = default) =>
        await PauseAsync(GetWorkerServices(null), cancellationToken).ConfigureAwait(false);

    public async ValueTask PauseWorkerAsync(OutboxSettings outboxSettings, CancellationToken cancellationToken = default) =>
        await PauseAsync(GetWorkerServices(Check.NotNull(outboxSettings, nameof(outboxSettings))), cancellationToken).ConfigureAwait(false);

    public void ResumeWorker() => GetWorkerServices(null).ForEach(service => service.ResumeAutomaticProcessing());

    public void ResumeWorker(OutboxSettings outboxSettings) =>
        GetWorkerServices(Check.NotNull(outboxSettings, nameof(outboxSettings))).ForEach(service => service.ResumeAutomaticProcessing());

    /// <summary>
    ///     Checks whether the outbox is empty, logging and ignoring the errors (returning <c>false</c>). When no readers are specified,
    ///     only the outboxes that are actually being drained (having a registered and not suspended worker) are considered.
    /// </summary>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    internal async ValueTask<bool> IsEmptyIgnoringErrorsAsync(IReadOnlyCollection<IOutboxReader>? readers)
    {
        try
        {
            if (readers != null)
            {
                foreach (IOutboxReader reader in readers)
                {
                    if (await reader.GetLengthAsync().ConfigureAwait(false) > 0)
                        return false;
                }

                return true;
            }

            IEnumerable<OutboxWorkerService> activeServices = _serviceProvider
                .GetServices<IHostedService>()
                .OfType<OutboxWorkerService>()
                .Where(service => service.IsEnabled && !service.IsAutomaticProcessingSuspended);

            foreach (OutboxWorkerService service in activeServices)
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

    private static async ValueTask PauseAsync(List<OutboxWorkerService> services, CancellationToken cancellationToken)
    {
        // Suspend all the workers right away, then wait for the running passes
        services.ForEach(service => service.SuspendAutomaticProcessing());

        foreach (OutboxWorkerService service in services)
        {
            await service.WaitForRunningPassAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<List<OutboxMessage>> ReadAsync(IOutboxReader reader, int count)
    {
        List<OutboxMessage> messages = [];

        using IDisposableAsyncEnumerable<OutboxMessage> enumerable = await reader.GetAsync(count).ConfigureAwait(false);

        await foreach (OutboxMessage message in enumerable.ConfigureAwait(false))
        {
            messages.Add(message);
        }

        return messages;
    }

    private List<OutboxWorkerService> GetWorkerServices(OutboxSettings? outboxSettings)
    {
        List<OutboxWorkerService> services =
        [
            .. _serviceProvider
                .GetServices<IHostedService>()
                .OfType<OutboxWorkerService>()
                .Where(service => outboxSettings == null || service.Settings.Outbox == outboxSettings)
        ];

        if (services.Count == 0)
        {
            throw new InvalidOperationException(
                outboxSettings == null
                    ? "No outbox worker is registered."
                    : "No outbox worker is registered for the specified outbox.");
        }

        return services;
    }

    private IEnumerable<IOutboxReader> GetReaders(OutboxSettings? outboxSettings)
    {
        OutboxReaderFactory readerFactory = _serviceProvider.GetRequiredService<OutboxReaderFactory>();

        return GetOutboxSettings(outboxSettings).Select(settings => readerFactory.GetReader(settings, _serviceProvider));
    }

    private List<OutboxSettings> GetOutboxSettings(OutboxSettings? outboxSettings)
    {
        if (outboxSettings != null)
            return [outboxSettings];

        IEnumerable<OutboxSettings> workersOutboxes = _serviceProvider
            .GetServices<IHostedService>()
            .OfType<OutboxWorkerService>()
            .Select(service => service.Settings.Outbox);

        IEnumerable<IProducer> producers = _serviceProvider.GetService<IProducerCollection>() ?? Enumerable.Empty<IProducer>();

        IEnumerable<OutboxSettings> producersOutboxes = producers
            .Select(producer => producer.EndpointConfiguration.Strategy)
            .OfType<OutboxProduceStrategy>()
            .Select(strategy => strategy.Settings);

        return [.. workersOutboxes.Concat(producersOutboxes).Distinct()];
    }
}
