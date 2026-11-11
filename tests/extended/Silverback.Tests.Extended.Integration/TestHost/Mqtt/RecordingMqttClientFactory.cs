// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Linq;
using MQTTnet;
using Silverback.Messaging.Broker.Mqtt;

namespace Silverback.Tests.Extended.Integration.TestHost.Mqtt;

/// <summary>
///     Creates the native MQTT clients used by Silverback, recording the packets of each of them.
/// </summary>
internal sealed class RecordingMqttClientFactory : IMqttNetClientFactory, IDisposable
{
    private readonly ConcurrentQueue<MqttClientRecorder> _recorders = new();

    public IMqttClient CreateClient()
    {
        MqttClientRecorder recorder = new(new MqttClientFactory().CreateMqttClient());
        _recorders.Enqueue(recorder);

        return recorder.Client;
    }

    public MqttClientRecorder GetRecorder(string clientId) =>
        FindRecorder(clientId) ?? throw new InvalidOperationException($"The client {clientId} wasn't connected.");

    // The client identifier is known once the client is connected
    public MqttClientRecorder? FindRecorder(string clientId) =>
        _recorders.SingleOrDefault(recorder => recorder.Client.Options?.ClientId == clientId);

    public void Dispose()
    {
        foreach (MqttClientRecorder recorder in _recorders)
        {
            recorder.Client.Dispose();
        }
    }
}
