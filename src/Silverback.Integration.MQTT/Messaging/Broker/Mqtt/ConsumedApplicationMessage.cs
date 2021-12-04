// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using MQTTnet;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker.Mqtt;

internal sealed class ConsumedApplicationMessage
{
    public ConsumedApplicationMessage(MqttApplicationMessage applicationMessage)
    {
        ApplicationMessage = applicationMessage;
        Id = applicationMessage.UserProperties
                 ?.FirstOrDefault(header => header.Name == DefaultMessageHeaders.MessageId)?.Value
             ?? Guid.NewGuid().ToString();
    }

    public string Id { get; }

    public MqttApplicationMessage ApplicationMessage { get; }

    public TaskCompletionSource<bool> TaskCompletionSource { get; set; } = new();
}
