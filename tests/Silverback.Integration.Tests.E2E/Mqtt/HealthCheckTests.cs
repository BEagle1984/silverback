// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.HealthChecks;
using Silverback.Tests.Integration.E2E.TestHost;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public class HealthCheckTests : MqttTests
{
    public HealthCheckTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task ConsumerHealthCheck_ShouldReturnHealthyStatus_WhenAllConsumersConnected()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom("topic1")
                        .ConsumeFrom("topic2", "topic3")))
                .AddClient(client => client
                    .WithClientId("e2e-client-2")
                    .Consume(endpoint => endpoint
                        .ConsumeFrom("topic4"))))
            .Services
            .AddHealthChecks()
            .AddConsumersCheck(gracePeriod: TimeSpan.Zero));

        Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Count.ShouldBe(2);

        HttpResponseMessage response = await Host.HttpClient.GetAsync("/health");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(500)]
    public async Task ConsumerHealthCheck_ShouldReturnUnhealthy_WhenClientsNotConnectedAndGracePeriodElapsed(int requestDelayMilliseconds)
    {
        TimeSpan gracePeriod = TimeSpan.FromMilliseconds(300);

        await Host.ConfigureServices(services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt(mockedMqttOptions =>
                    mockedMqttOptions.DelayConnection(TimeSpan.FromHours(1))))
                .AddMqttClients(clients => clients
                    .ConnectViaTcp("e2e-mqtt-broker")
                    .AddClient(client => client
                        .WithClientId(DefaultClientId)
                        .Consume(endpoint => endpoint
                            .ConsumeFrom("topic1")
                            .ConsumeFrom("topic2", "topic3")))
                    .AddClient(client => client
                        .WithClientId("e2e-client-2")
                        .Consume(endpoint => endpoint
                            .ConsumeFrom("topic4"))))
                .Services
                .AddHealthChecks()
                .AddConsumersCheck(gracePeriod: gracePeriod))
            .RunAsync(waitUntilBrokerClientsConnected: false);

        IConsumerCollection consumers = Host.ServiceProvider.GetRequiredService<IConsumerCollection>();
        await AsyncTestingUtil.WaitAsync(() =>
            consumers.Count == 2 && consumers.All(consumer => consumer.StatusInfo.Status == ConsumerStatus.Started));

        consumers.Count.ShouldBe(2);
        consumers.ShouldAllBe(consumer => consumer.StatusInfo.Status == ConsumerStatus.Started);

        // The first request may be scheduled after startup grace has already expired
        await Task.Delay(requestDelayMilliseconds);

        await AsyncTestingUtil.WaitAsync(() => consumers.All(consumer =>
            consumer.StatusInfo.History.Last().Timestamp < DateTime.UtcNow.Subtract(gracePeriod)));

        foreach (IConsumer consumer in consumers)
        {
            consumer.StatusInfo.History.Last().Timestamp.ShouldNotBeNull().ShouldBeLessThan(DateTime.UtcNow.Subtract(gracePeriod));
            consumer.ShouldBeOfType<MqttConsumer>().Client.IsConnected.ShouldBeFalse();
        }

        using HttpResponseMessage response = await Host.HttpClient.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        consumers.ShouldAllBe(consumer => consumer.StatusInfo.Status == ConsumerStatus.Started);
    }
}
