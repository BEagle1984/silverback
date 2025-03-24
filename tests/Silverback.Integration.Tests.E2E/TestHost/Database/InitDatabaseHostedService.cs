// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Silverback.Storage;

namespace Silverback.Tests.Integration.E2E.TestHost.Database;

internal class InitDatabaseHostedService : IHostedService
{
    private readonly SilverbackStorageInitializer _storageInitializer;

    private readonly Func<SilverbackStorageInitializer, Task> _initFunction;

    public InitDatabaseHostedService(
        SilverbackStorageInitializer storageInitializer,
        Func<SilverbackStorageInitializer, Task> initFunction)
    {
        _storageInitializer = storageInitializer;
        _initFunction = initFunction;
    }

    public Task StartAsync(CancellationToken cancellationToken) => _initFunction.Invoke(_storageInitializer);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
