// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using MQTTnet;

namespace Silverback.Messaging.Broker.Mqtt
{
    internal class ConsumedApplicationMessage
    {
        public ConsumedApplicationMessage(MqttApplicationMessage applicationMessage)
        {
            ApplicationMessage = applicationMessage;
        }

        public Guid Id { get; } = Guid.NewGuid();

        public MqttApplicationMessage ApplicationMessage { get; }

        public TaskCompletionSource<bool> TaskCompletionSource { get; } = new();
    }
}
