// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using MQTTnet.Client.Options;
using MQTTnet.Packets;

namespace Silverback.Tools.Generators.MqttConfigProxies;

internal static class Program
{
    private static void Main()
    {
        State state = new();

        state.AddType<MqttClientOptions>();
        state.AddType<MqttUserProperty>();
        state.AddType<MqttClientCredentials>();
        state.AddType<MqttClientTcpOptions>();
        state.AddType<MqttClientWebSocketOptions>();
        state.AddType<MqttClientTlsOptions>();
        state.AddType<MqttClientWebSocketProxyOptions>();

        while (state.GeneratorQueue.TryDequeue(out Type? type))
        {
            Console.Write(new ProxyClassGenerator(type, state).Generate());
            Console.WriteLine();
        }

        Console.Write(new BuilderGenerator("MqttClientConfigurationBuilder").Generate());
        Console.WriteLine();

        Console.Write(new BuilderGenerator("MqttClientsConfigurationBuilder").Generate());
        Console.WriteLine();
    }
}
