// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Util;

namespace Silverback.Testing;

/// <summary>
///     Suspends the automatic processing of all the <see cref="OutboxWorkerService" /> instances, before they are started.
/// </summary>
internal sealed class ManualOutboxWorkerLifecycleService : IHostedLifecycleService
{
    private readonly IServiceProvider _serviceProvider;

    public ManualOutboxWorkerLifecycleService(IServiceProvider serviceProvider)
    {
        _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
    }

    public Task StartingAsync(CancellationToken cancellationToken)
    {
        // The workers aren't started yet, so there is no running pass to wait for
        foreach (OutboxWorkerService service in _serviceProvider.GetServices<IHostedService>().OfType<OutboxWorkerService>())
        {
            service.SuspendAutomaticProcessing();
        }

        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
