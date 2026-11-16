// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Confluent.SchemaRegistry;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class TestingHelperTests
{
    [Fact]
    public async Task GetProducerForEndpoint_ShouldMirrorAvroDeserializer_WhenNotConfigured()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka().AddMockedConfluentSchemaRegistry())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<AvroMessage>(endpoint => endpoint
                        .DeserializeAvro(avro => avro.ConnectToSchemaRegistry("http://e2e:4242"))
                        .ConsumeFrom(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint(DefaultTopicName).ProduceAsync(new AvroMessage { number = "42" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem().Message.ShouldBeOfType<AvroMessage>().number.ShouldBe("42");
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldMirrorAvroDeserializer_WhenDeserializerSpecificSettingsAreSet()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka().AddMockedConfluentSchemaRegistry())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<AvroMessage>(endpoint => endpoint
                        .DeserializeAvro(avro => avro
                            .ConnectToSchemaRegistry("http://e2e:4242")
                            .Configure(config => config
                                .UseLatestVersion()
                                .WithSubjectNameStrategy(SubjectNameStrategy.Topic)))
                        .ConsumeFrom(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint(DefaultTopicName).ProduceAsync(new AvroMessage { number = "42" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem().Message.ShouldBeOfType<AvroMessage>().number.ShouldBe("42");
    }
}
