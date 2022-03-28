// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.HealthChecks;

/// <inheritdoc cref="IProducersHealthCheckService" />
public class ProducersHealthCheckService : IProducersHealthCheckService
{
    private readonly IProducerCollection _producers;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProducersHealthCheckService" /> class.
    /// </summary>
    /// <param name="producers">
    ///     The collection containing all configured producers.
    /// </param>
    public ProducersHealthCheckService(IProducerCollection producers)
    {
        _producers = Check.NotNull(producers, nameof(producers));
    }

    /// <inheritdoc cref="IProducersHealthCheckService.SendPingMessagesAsync" />
    [SuppressMessage("", "CA1031", Justification = "Exception is returned")]
    public async Task<IReadOnlyCollection<EndpointCheckResult>> SendPingMessagesAsync()
    {
        if (!_producers.All(producer => producer.Client.Status is ClientStatus.Initialized or ClientStatus.Initializing))
            return Array.Empty<EndpointCheckResult>();

        IEnumerable<Task<EndpointCheckResult>> tasks = _producers.Select(PingEndpointAsync);
        return await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    [SuppressMessage("", "CA1031", Justification = "Exception reported in the result.")]
    private static async Task<EndpointCheckResult> PingEndpointAsync(IProducer producer)
    {
        try
        {
            await producer.ProduceAsync(PingMessage.New()).ConfigureAwait(false);
            return new EndpointCheckResult(producer.EndpointConfiguration.DisplayName, true);
        }
        catch (Exception ex)
        {
            return new EndpointCheckResult(producer.EndpointConfiguration.DisplayName, false, $"[{ex.GetType().FullName}] {ex.Message}");
        }
    }
}
