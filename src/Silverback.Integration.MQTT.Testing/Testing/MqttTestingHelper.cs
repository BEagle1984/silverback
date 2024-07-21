// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Mqtt.Mocks;
using Silverback.Util;

namespace Silverback.Testing;

/// <inheritdoc cref="IMqttTestingHelper" />
public partial class MqttTestingHelper : TestingHelper, IMqttTestingHelper
{
    private readonly IServiceProvider _serviceProvider;

    private readonly IInMemoryMqttBroker? _inMemoryMqttBroker;

    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttTestingHelper" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" />.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="ISilverbackLogger" />.
    /// </param>
    public MqttTestingHelper(
        IServiceProvider serviceProvider,
        ILogger<MqttTestingHelper> logger)
        : base(serviceProvider, logger)
    {
        _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        _inMemoryMqttBroker = serviceProvider.GetService<IInMemoryMqttBroker>();
        _serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
    }

    /// <inheritdoc cref="IMqttTestingHelper.GetClientSession" />
    public IClientSession GetClientSession(string clientId)
    {
        if (_inMemoryMqttBroker == null)
            throw new InvalidOperationException("The IInMemoryMqttBroker is not initialized.");

        return _inMemoryMqttBroker.GetClientSession(clientId);
    }

    /// <inheritdoc cref="IMqttTestingHelper.GetMessages" />
    public IReadOnlyList<MqttApplicationMessage> GetMessages(string topic)
    {
        if (_inMemoryMqttBroker == null)
            throw new InvalidOperationException("The IInMemoryMqttBroker is not initialized.");

        return _inMemoryMqttBroker.GetMessages(topic);
    }

    /// <inheritdoc cref="TestingHelper.WaitUntilAllMessagesAreConsumedCoreAsync(CancellationToken)" />
    protected override Task WaitUntilAllMessagesAreConsumedCoreAsync(CancellationToken cancellationToken) =>
        _inMemoryMqttBroker == null
            ? Task.CompletedTask
            : _inMemoryMqttBroker.WaitUntilAllMessagesAreConsumedAsync(cancellationToken);
}
