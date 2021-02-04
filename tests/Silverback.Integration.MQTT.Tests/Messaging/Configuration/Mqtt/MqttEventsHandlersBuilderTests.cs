// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt
{
    public class MqttEventsHandlersBuilderTests
    {
        [Fact]
        public void Build_ShouldBuild()
        {
            Func<MqttClientConfig, Task> connectedCallback = _ => Task.CompletedTask;
            Func<MqttClientConfig, Task> disconnectingCallback = _ => Task.CompletedTask;
            MqttEventsHandlersBuilder builder = new();

            builder
                .OnConnected(connectedCallback)
                .OnDisconnecting(disconnectingCallback);

            MqttEventsHandlers events = builder.Build();

            events.ConnectedHandler.Should().BeSameAs(connectedCallback);
            events.DisconnectingHandler.Should().BeSameAs(disconnectingCallback);
        }
    }
}
