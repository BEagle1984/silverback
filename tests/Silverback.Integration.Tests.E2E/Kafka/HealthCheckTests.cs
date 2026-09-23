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
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class HealthCheckTests : KafkaTests
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
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom("topic1"))
                    .Consume(endpoint => endpoint.ConsumeFrom("topic2", "topic3")))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom("topic4"))))
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
    public async Task ConsumerHealthCheck_ShouldReturnUnhealthy_WhenPartitionsNotAssignedAndGracePeriodElapsed(int requestDelayMilliseconds)
    {
        TimeSpan gracePeriod = TimeSpan.FromMilliseconds(300);

        await Host.ConfigureServices(services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka(mockedKafkaOptions =>
                    mockedKafkaOptions.DelayPartitionsAssignment(TimeSpan.FromHours(1))))
                .AddKafkaClients(clients => clients
                    .WithBootstrapServers("PLAINTEXT://e2e")
                    .AddConsumer(consumer => consumer
                        .WithGroupId(DefaultGroupId)
                        .Consume(endpoint => endpoint.ConsumeFrom("topic1"))
                        .Consume(endpoint => endpoint.ConsumeFrom("topic2", "topic3")))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(DefaultGroupId)
                        .Consume(endpoint => endpoint.ConsumeFrom("topic4"))))
                .Services
                .AddHealthChecks()
                .AddConsumersCheck(gracePeriod: gracePeriod))
            .RunAsync(waitUntilBrokerClientsConnected: false);

        IConsumerCollection consumers = Host.ServiceProvider.GetRequiredService<IConsumerCollection>();
        await AsyncTestingUtil.WaitAsync(() => consumers.Count == 2 && consumers.All(consumer => consumer.StatusInfo.Status == ConsumerStatus.Started));

        consumers.Count.ShouldBe(2);
        consumers.ShouldAllBe(consumer => consumer.StatusInfo.Status == ConsumerStatus.Started);

        // The first request may be scheduled after startup grace has already expired
        await Task.Delay(requestDelayMilliseconds);

        await AsyncTestingUtil.WaitAsync(() => consumers.All(consumer =>
            consumer.StatusInfo.History.Last().Timestamp < DateTime.UtcNow.Subtract(gracePeriod)));

        foreach (IConsumer consumer in consumers)
        {
            consumer.StatusInfo.History.Last().Timestamp.ShouldNotBeNull().ShouldBeLessThan(DateTime.UtcNow.Subtract(gracePeriod));
            consumer.ShouldBeOfType<KafkaConsumer>().Client.Assignment.ShouldBeEmpty();
        }

        using HttpResponseMessage response = await Host.HttpClient.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        consumers.ShouldAllBe(consumer => consumer.StatusInfo.Status == ConsumerStatus.Started);
    }
}
