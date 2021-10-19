// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class MultipleBrokersTests : KafkaTestFixture
{
    public MultipleBrokersTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task MultipleBrokers_OverlappingTopicNames_CorrectlyProducedAndConsumed()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://tests-1";
                                })
                            .AddOutbound<Broker1Message>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = "group1";
                                        })))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://tests-2";
                                })
                            .AddOutbound<Broker2Message>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = "group2";
                                        })))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(new Broker1Message());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        await publisher.PublishAsync(new Broker2Message());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<Broker1Message>();
        Helper.Spy.InboundEnvelopes[0].Endpoint.Configuration.As<KafkaConsumerConfiguration>()
            .Client.BootstrapServers.Should().Be("PLAINTEXT://tests-1");
        Helper.Spy.InboundEnvelopes[1].Message.Should().BeOfType<Broker2Message>();
        Helper.Spy.InboundEnvelopes[1].Endpoint.Configuration.As<KafkaConsumerConfiguration>()
            .Client.BootstrapServers.Should().Be("PLAINTEXT://tests-2");
    }

    [Fact]
    public async Task MultipleBrokers_OverlappingTopicAndGroupNames_CorrectlyProducedAndConsumed()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://tests-1";
                                })
                            .AddOutbound<Broker1Message>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                        })))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://tests-2";
                                })
                            .AddOutbound<Broker2Message>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                        })))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(new Broker1Message());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        await publisher.PublishAsync(new Broker2Message());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<Broker1Message>();
        Helper.Spy.InboundEnvelopes[0].Endpoint.Configuration.As<KafkaConsumerConfiguration>()
            .Client.BootstrapServers.Should().Be("PLAINTEXT://tests-1");
        Helper.Spy.InboundEnvelopes[1].Message.Should().BeOfType<Broker2Message>();
        Helper.Spy.InboundEnvelopes[1].Endpoint.Configuration.As<KafkaConsumerConfiguration>()
            .Client.BootstrapServers.Should().Be("PLAINTEXT://tests-2");
    }

    private sealed class Broker1Message : IIntegrationMessage
    {
    }

    private sealed class Broker2Message : IIntegrationMessage
    {
    }
}
