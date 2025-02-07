// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;

namespace Silverback.Messaging.Broker.Mqtt;

internal sealed class ConsumedApplicationMessage
{
    private readonly MqttApplicationMessageReceivedEventArgs _eventArgs;

    public ConsumedApplicationMessage(MqttApplicationMessageReceivedEventArgs eventArgs)
    {
        _eventArgs = eventArgs;
        Id = Guid.NewGuid().ToString();
    }

    public string Id { get; }

    public MqttApplicationMessage ApplicationMessage => _eventArgs.ApplicationMessage;

    public TaskCompletionSource<bool> TaskCompletionSource { get; set; } = new();

    public Task AcknowledgeAsync(CancellationToken cancellationToken) => _eventArgs.AcknowledgeAsync(cancellationToken);
}
