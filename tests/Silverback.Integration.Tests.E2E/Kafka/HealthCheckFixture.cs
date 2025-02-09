// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Integration.E2E.TestHost;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class HealthCheckFixture : KafkaFixture
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
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom("topic1"))
                                .Consume(endpoint => endpoint.ConsumeFrom("topic2", "topic3")))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom("topic4"))))
                .Services
                .AddHealthChecks()
                .AddConsumersCheck());

        HttpResponseMessage response = await Host.HttpClient.GetAsync("/health");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ConsumerHealthCheck_FailingToAssignPartitions_UnhealthyReturnedAfterGracePeriod()
    {
        await Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                        .WithConnectionToMessageBroker(
                        options => options.AddMockedKafka(
                            mockedKafkaOptions =>
                                mockedKafkaOptions.DelayPartitionsAssignment(TimeSpan.FromHours(1))))
                    .AddKafkaClients(
                        clients => clients
                            .WithBootstrapServers("PLAINTEXT://e2e")
                            .AddConsumer(
                                consumer => consumer
                                    .WithGroupId(DefaultGroupId)
                                    .Consume(endpoint => endpoint.ConsumeFrom("topic1"))
                                    .Consume(endpoint => endpoint.ConsumeFrom("topic2", "topic3")))
                            .AddConsumer(
                                consumer => consumer
                                    .WithGroupId(DefaultGroupId)
                                    .Consume(endpoint => endpoint.ConsumeFrom("topic4"))))
                    .Services
                    .AddHealthChecks()
                    .AddConsumersCheck(gracePeriod: TimeSpan.FromMilliseconds(300)))
            .RunAsync(waitUntilBrokerClientsConnected: false);

        HttpResponseMessage response = await Host.HttpClient.GetAsync("/health");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await Task.Delay(500); // Incremented this to 500 because flaky

        response = await Host.HttpClient.GetAsync("/health");
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }
}
