// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.HealthChecks;

/// <inheritdoc cref="IConsumersHealthCheckService" />
public class ConsumersHealthCheckService : IConsumersHealthCheckService
{
    private readonly IConsumerCollection _consumerCollection;

    private bool _applicationIsStopping;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConsumersHealthCheckService" /> class.
    /// </summary>
    /// <param name="consumerCollection">
    ///     The collection holding a reference to all consumers.
    /// </param>
    /// <param name="applicationLifetime">
    ///     The <see cref="IHostApplicationLifetime" /> used to track the application shutdown.
    /// </param>
    public ConsumersHealthCheckService(IConsumerCollection consumerCollection, IHostApplicationLifetime applicationLifetime)
    {
        _consumerCollection = Check.NotNull(consumerCollection, nameof(consumerCollection));
        Check.NotNull(applicationLifetime, nameof(applicationLifetime));

        applicationLifetime.ApplicationStopping.Register(() => _applicationIsStopping = true);
    }

    /// <inheritdoc cref="IConsumersHealthCheckService.GetDisconnectedConsumersAsync" />
    public Task<IReadOnlyCollection<IConsumer>> GetDisconnectedConsumersAsync(ConsumerStatus minStatus, TimeSpan gracePeriod)
    {
        // The check is skipped when the application is shutting down, because all consumers will be
        // disconnected and since the shutdown could take a while we don't want to report the application
        // as unhealthy.
        if (_applicationIsStopping)
            return Task.FromResult((IReadOnlyCollection<IConsumer>)[]);

        IReadOnlyCollection<IConsumer> disconnectedConsumers =
            _consumerCollection
                .Where(consumer => IsDisconnected(consumer, minStatus, gracePeriod))
                .ToList();

        return Task.FromResult(disconnectedConsumers);
    }

    private static bool IsDisconnected(IConsumer consumer, ConsumerStatus minStatus, TimeSpan gracePeriod) =>
        consumer.StatusInfo.Status < minStatus &&
        (gracePeriod == TimeSpan.Zero || consumer.StatusInfo.History.Count == 0 || GracePeriodElapsed(consumer, gracePeriod));

    private static bool GracePeriodElapsed(IConsumer consumer, TimeSpan gracePeriod) =>
        consumer.StatusInfo.History.Last().Timestamp < DateTime.UtcNow.Subtract(gracePeriod);
}
