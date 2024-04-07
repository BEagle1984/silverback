// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Integration.E2E.TestHost;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public class HealthCheckFixture : MqttFixture
{
    public HealthCheckFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task ConsumerHealthCheck_ShouldReturnHealthyStatus_WhenAllConsumersConnected()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom("topic1")
                                        .ConsumeFrom("topic2", "topic3")))
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom("topic4"))))
                .Services
                .AddHealthChecks()
                .AddConsumersCheck());

        HttpResponseMessage response = await Host.HttpClient.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // TODO: Test unhealthy case
}
