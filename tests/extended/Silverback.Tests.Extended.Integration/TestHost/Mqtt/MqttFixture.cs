// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Exceptions;

namespace Silverback.Tests.Extended.Integration.TestHost.Mqtt;

public sealed class MqttFixture : DockerTestsFixture
{
    public const string BrokerHost = "localhost";

    protected override IReadOnlyCollection<string> InfrastructureServices => ["emqx1", "emqx2", "haproxy"];

    protected override async Task WaitForInfrastructureAsync()
    {
        using IMqttClient client = new MqttClientFactory().CreateMqttClient();
        Stopwatch stopwatch = Stopwatch.StartNew();
        MqttClientOptions options = new MqttClientOptionsBuilder()
            .WithTcpServer(BrokerHost)
            .WithClientId("stress-ready-" + Guid.NewGuid().ToString("N"))
            .Build();

        while (stopwatch.Elapsed < TimeSpan.FromMinutes(2))
        {
            try
            {
                using CancellationTokenSource cancellation = new(TimeSpan.FromSeconds(5));
                await client.ConnectAsync(options, cancellation.Token);
                await client.DisconnectAsync(cancellationToken: cancellation.Token);

                return;
            }
            catch (MqttCommunicationException)
            {
                // The MQTT cluster and its proxy must accept connections before starting the tests
            }
            catch (OperationCanceledException)
            {
                // A connection attempt can time out while the broker is starting
            }

            await Task.Delay(250);
        }

        throw new TimeoutException("The root compose MQTT cluster did not become ready.");
    }
}
