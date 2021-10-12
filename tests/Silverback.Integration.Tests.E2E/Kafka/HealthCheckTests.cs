// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestHost;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
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
                                    mockedKafkaOptions.DelayPartitionsAssignment(
                                        TimeSpan.FromMilliseconds(100))))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom("topic1")
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            }))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom("topic2")
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            }))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom("topic3")
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            })))
                        .Services
                        .AddHealthChecks()
                        .AddConsumersCheck())
                .Run(waitUntilBrokerConnected: false);

            var response = await Host.HttpClient.GetAsync("/health");
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
                                mockedKafkaOptions =>
                                    mockedKafkaOptions.DelayPartitionsAssignment(
                                        TimeSpan.FromMilliseconds(10000)))) // Delay the assignment on purpose
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom("topic1")
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            }))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom("topic2")
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            }))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom("topic3")
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            })))
                        .Services
                        .AddHealthChecks()
                        .AddConsumersCheck(gracePeriod: TimeSpan.FromMilliseconds(100)))
                .Run(waitUntilBrokerConnected: false);

            var response = await Host.HttpClient.GetAsync("/health");
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
                                    mockedKafkaOptions.DelayPartitionsAssignment(
                                        TimeSpan.FromMilliseconds(200))))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://e2e";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom("topic1")
                                        .WithName("one")
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            }))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom("topic2")
                                        .WithName("two")
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            }))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom("topic3")
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            })))
                        .Services
                        .AddHealthChecks()
                        .AddConsumersCheck(
                            endpointsFilter: endpoint => endpoint.FriendlyName == "two",
                            gracePeriod: TimeSpan.Zero,
                            name: "check-2",
                            tags: new[] { "1" })
                        .AddConsumersCheck(
                            endpointsFilter: endpoint => endpoint.FriendlyName != "two",
                            gracePeriod: TimeSpan.Zero,
                            name: "check-1-3",
                            tags: new[] { "2" }))
                .Run(waitUntilBrokerConnected: false);

            var consumers = Helper.Broker.Consumers;
            await AsyncTestingUtil.WaitAsync(
                () => consumers.All(consumer => consumer.StatusInfo.Status > ConsumerStatus.Connected));

            var response = await Host.HttpClient.GetAsync("/health1");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response = await Host.HttpClient.GetAsync("/health2");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            await consumers[1].DisconnectAsync();

            response = await Host.HttpClient.GetAsync("/health1");
            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            response = await Host.HttpClient.GetAsync("/health2");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            await consumers[1].ConnectAsync();
            await AsyncTestingUtil.WaitAsync(
                () => consumers[1].StatusInfo.Status > ConsumerStatus.Connected);
            await consumers[2].DisconnectAsync();

            response = await Host.HttpClient.GetAsync("/health1");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response = await Host.HttpClient.GetAsync("/health2");
            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        }
    }
}
