// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text.Json;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Protocol;
using Shouldly;

namespace Silverback.Tests.Extended.Integration.TestHost.Mqtt;

// A client that isn't managed by Silverback, created by MqttTestHarness to publish the input of the test or to observe its output.
// Subscribe through this client rather than through the native one, since it returns only once the subscription reached all the
// cluster nodes (see MqttFixture.WaitUntilSubscriptionsPropagatedAsync).
public sealed class TestMqttClient : IAsyncDisposable
{
    private readonly TimeSpan _timeout;

    public TestMqttClient(IMqttClient client, TimeSpan timeout)
    {
        Native = client;
        _timeout = timeout;
    }

    // The underlying MQTTnet client, to observe its events
    public IMqttClient Native { get; }

    public async Task SubscribeAsync(string topicFilter, MqttQualityOfServiceLevel qualityOfServiceLevel)
    {
        MqttClientSubscribeResult result = await Native.SubscribeAsync(topicFilter, qualityOfServiceLevel).WaitAsync(_timeout);

        // A rejected subscription would never be routed
        result.Items.ShouldAllBe(item => item.ResultCode <= MqttClientSubscribeResultCode.GrantedQoS2);

        await MqttFixture.WaitUntilSubscriptionsPropagatedAsync(Native.Options.ClientId, [topicFilter]);
    }

    public Task PublishJsonAsync<TMessage>(
        string topic,
        TMessage message,
        MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce) =>
        Native.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(JsonSerializer.SerializeToUtf8Bytes(message))
            .WithQualityOfServiceLevel(qualityOfServiceLevel)
            .Build()).WaitAsync(_timeout);

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (Native.IsConnected)
                await Native.DisconnectAsync().WaitAsync(_timeout);
        }
        finally
        {
            Native.Dispose();
        }
    }
}
