// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using Confluent.SchemaRegistry;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class SchemaRegistryFixture
{
    [Fact]
    public async Task SchemaRegistry_ShouldProduceAndConsumeProtobuf()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka().AddMockedConfluentSchemaRegistry())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<ProtobufMessage>(
                                    endpoint => endpoint
                                        .SerializeAsProtobuf(
                                            json => json
                                                .ConnectToSchemaRegistry("http://e2e:4242")
                                                .Configure(
                                                    config =>
                                                    {
                                                        config.AutoRegisterSchemas = false;
                                                    }))
                                        .ProduceTo(DefaultTopicName)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume<ProtobufMessage>(
                                    endpoint => endpoint
                                        .DeserializeProtobuf(
                                            json => json
                                                .ConnectToSchemaRegistry("http://e2e:4242"))
                                        .ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IConfluentSchemaRegistryClientFactory schemaRegistryClientFactory = Host.ServiceProvider.GetRequiredService<IConfluentSchemaRegistryClientFactory>();
        ISchemaRegistryClient schemaRegistryClient = schemaRegistryClientFactory.GetClient(registry => registry.WithUrl("http://e2e:4242"));
        await schemaRegistryClient.RegisterSchemaAsync(
            DefaultTopicName + "-value",
            new Schema(ProtobufMessage.Schema, SchemaType.Protobuf));

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishAsync(new ProtobufMessage { Number = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(15);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(15);
        Helper.Spy.InboundEnvelopes.Select(envelope => ((ProtobufMessage)envelope.Message!).Number)
            .ShouldBe(Enumerable.Range(1, 15).Select(i => $"{i}"), ignoreOrder: true);
    }

    [Fact]
    public async Task SchemaRegistry_ShouldProduceAndConsumeProtobuf_WhenAutoRegistering()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka().AddMockedConfluentSchemaRegistry())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<ProtobufMessage>(
                                    endpoint => endpoint
                                        .SerializeAsProtobuf(json => json.ConnectToSchemaRegistry("http://e2e:4242"))
                                        .ProduceTo(DefaultTopicName)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume<ProtobufMessage>(
                                    endpoint => endpoint
                                        .DeserializeProtobuf(json => json.ConnectToSchemaRegistry("http://e2e:4242"))
                                        .ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishAsync(new ProtobufMessage { Number = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(15);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(15);
        Helper.Spy.InboundEnvelopes.Select(envelope => ((ProtobufMessage)envelope.Message!).Number)
            .ShouldBe(Enumerable.Range(1, 15).Select(i => $"{i}"), ignoreOrder: true);
    }

    [Fact]
    public async Task SchemaRegistry_ShouldProduceAndConsumeProtobuf_WhenUsingLatestSchema()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka().AddMockedConfluentSchemaRegistry())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<ProtobufMessage>(
                                    endpoint => endpoint
                                        .SerializeAsProtobuf(
                                            json => json
                                                .ConnectToSchemaRegistry("http://e2e:4242")
                                                .Configure(
                                                    config =>
                                                    {
                                                        config.AutoRegisterSchemas = false;
                                                        config.UseLatestVersion = true;
                                                    }))
                                        .ProduceTo(DefaultTopicName)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume<ProtobufMessage>(
                                    endpoint => endpoint
                                        .DeserializeProtobuf(
                                            json => json
                                                .ConnectToSchemaRegistry("http://e2e:4242").Configure(
                                                    config =>
                                                    {
                                                        config.UseLatestVersion = true;
                                                    }))
                                        .ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IConfluentSchemaRegistryClientFactory schemaRegistryClientFactory = Host.ServiceProvider.GetRequiredService<IConfluentSchemaRegistryClientFactory>();
        ISchemaRegistryClient schemaRegistryClient = schemaRegistryClientFactory.GetClient(registry => registry.WithUrl("http://e2e:4242"));
        await schemaRegistryClient.RegisterSchemaAsync(
            DefaultTopicName + "-value",
            new Schema(ProtobufMessage.Schema, SchemaType.Protobuf));

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishAsync(new ProtobufMessage { Number = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(15);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(15);
        Helper.Spy.InboundEnvelopes.Select(envelope => ((ProtobufMessage)envelope.Message!).Number)
            .ShouldBe(Enumerable.Range(1, 15).Select(i => $"{i}"), ignoreOrder: true);
    }
}
