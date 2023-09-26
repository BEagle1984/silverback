// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker.Mqtt
{
    internal sealed class ConsumedApplicationMessage
    {
        public ConsumedApplicationMessage(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            EventArgs = eventArgs;
            Id = Guid.NewGuid().ToString();
        }

        public string Id { get; }

        public MqttApplicationMessageReceivedEventArgs EventArgs { get; }

        public TaskCompletionSource<bool> TaskCompletionSource { get; set; } = new();
    }
}
