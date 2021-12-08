// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestHost;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class HealthCheckTests : KafkaTestFixture
{
    public HealthCheckTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task ConsumerHealthCheck_AllConnectedViaSubscription_HealthyReturned()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(
                        options => options.AddMockedKafka(
                            mockedKafkaOptions =>
                                mockedKafkaOptions.DelayPartitionsAssignment(TimeSpan.FromMilliseconds(100))))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom("topic1")
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId)))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom("topic2")
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId)))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom("topic3")
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .Services
                    .AddHealthChecks()
                    .AddConsumersCheck())
            .Run(waitUntilBrokerConnected: false);

        HttpResponseMessage response = await Host.HttpClient.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ConsumerHealthCheck_FailingToAssignPartitions_UnhealthyReturnedAfterGracePeriod()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(
                        options => options.AddMockedKafka(
                            mockOptions => mockOptions
                                .DelayPartitionsAssignment(TimeSpan.FromMilliseconds(10000)))) // Delay the assignment on purpose
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom("topic1")
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId)))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom("topic2")
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId)))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom("topic3")
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .Services
                    .AddHealthChecks()
                    .AddConsumersCheck(gracePeriod: TimeSpan.FromMilliseconds(100)))
            .Run(waitUntilBrokerConnected: false);

        HttpResponseMessage response = await Host.HttpClient.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await Task.Delay(100);

        response = await Host.HttpClient.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task ConsumerHealthCheck_SplitCheck_CorrectStatusReturned()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(
                        options => options.AddMockedKafka(
                            mockedKafkaOptions =>
                                mockedKafkaOptions.DelayPartitionsAssignment(TimeSpan.FromMilliseconds(200))))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom("topic1")
                                    .WithName("one")
                                    .ConfigureClient(configuration => configuration.WithGroupId("group1")))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom("topic2")
                                    .WithName("two")
                                    .ConfigureClient(configuration => configuration.WithGroupId("group2")))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom("topic3")
                                    .ConfigureClient(configuration => configuration.WithGroupId("group3"))))
                    .Services
                    .AddHealthChecks()
                    .AddConsumersCheck(
                        consumersFilter: endpoint => endpoint.FriendlyName == "two",
                        gracePeriod: TimeSpan.Zero,
                        name: "check-2",
                        tags: new[] { "1" })
                    .AddConsumersCheck(
                        consumersFilter: endpoint => endpoint.FriendlyName != "two",
                        gracePeriod: TimeSpan.Zero,
                        name: "check-1-3",
                        tags: new[] { "2" }))
            .Run(waitUntilBrokerConnected: false);

        IReadOnlyList<IConsumer> consumers = Helper.Broker.Consumers;
        await AsyncTestingUtil.WaitAsync(() => consumers.All(consumer => consumer.StatusInfo.Status > ConsumerStatus.Connected));

        HttpResponseMessage response = await Host.HttpClient.GetAsync("/health1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response = await Host.HttpClient.GetAsync("/health2");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await consumers[1].DisconnectAsync();

        response = await Host.HttpClient.GetAsync("/health1");
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response = await Host.HttpClient.GetAsync("/health2");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await consumers[1].ConnectAsync();
        await AsyncTestingUtil.WaitAsync(() => consumers[1].StatusInfo.Status > ConsumerStatus.Connected);
        await consumers[2].DisconnectAsync();

        response = await Host.HttpClient.GetAsync("/health1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response = await Host.HttpClient.GetAsync("/health2");
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}
