// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Testing;

/// <inheritdoc cref="ITestingHelper" />
public abstract partial class TestingHelper : ITestingHelper
{
    private readonly ILogger _logger;

    private readonly IServiceProvider _serviceProvider;

    private readonly IIntegrationSpy? _integrationSpy;

    private readonly IConsumerCollection? _consumers;

    private readonly IProducerCollection? _producers;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TestingHelper" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" />.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="ILogger{TCategoryName}" />.
    /// </param>
    protected TestingHelper(
        IServiceProvider serviceProvider,
        ILogger<TestingHelper> logger)
    {
        _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        _logger = logger;

        _integrationSpy = serviceProvider.GetService<IIntegrationSpy>();
        _consumers = serviceProvider.GetService<IConsumerCollection>();
        _producers = serviceProvider.GetService<IProducerCollection>();
    }

    /// <inheritdoc cref="ITestingHelper.Spy" />
    public IIntegrationSpy Spy => _integrationSpy ?? throw new InvalidOperationException(
        "The IIntegrationSpy couldn't be resolved. " +
        "Register it calling AddIntegrationSpy or AddIntegrationSpyAndSubscriber.");
}
