// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.BinaryMessages;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class TestingHelperTests
{
    private static readonly byte[] AesEncryptionKey = BytesUtil.GetRandomBytes(32);

    private static readonly byte[] AesEncryptionKey2 = BytesUtil.GetRandomBytes(32);

    [Theory]
    [InlineData(DefaultTopicName)]
    [InlineData("producer")]
    public async Task GetProducerForEndpoint_ShouldReturnConfiguredProducer_WhenProducerEndpointExists(string endpointName)
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<TestEventOne>("producer", endpoint => endpoint.ProduceTo(DefaultTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .DecryptUsingAes(AesEncryptionKey))))
            .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(endpointName);

        producer.ShouldBeSameAs(Host.ServiceProvider.GetRequiredService<IProducerCollection>().ShouldHaveSingleItem());
        producer.EndpointConfiguration.FriendlyName.ShouldBe("producer");
        producer.EndpointConfiguration.Encryption.ShouldBeNull();
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldThrow_WhenNoEndpointFound()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<TestEventOne>("producer", endpoint => endpoint.ProduceTo("topic1")))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume("consumer", endpoint => endpoint.ConsumeFrom("topic2"))))
            .AddIntegrationSpyAndSubscriber());

        Action act = () => Helper.GetProducerForEndpoint("topic3");

        act.ShouldThrow<InvalidOperationException>().Message.ShouldBe("No producer and no consumer found for endpoint 'topic3'.");
    }

    // The enumeration order of the configured producers depends on the registration order (e.g., reversed), so both registration orders are
    // covered, to be sure that the producer producing to the topic is enumerated before the one named after it in one of them
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetProducerForEndpoint_ShouldReturnProducerWithFriendlyName_WhenOtherProducerProducesToTopicWithSameName(bool topicProducerFirst)
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer =>
                {
                    if (topicProducerFirst)
                        producer.Produce<TestEventOne>("other", endpoint => endpoint.ProduceTo("orders"));

                    producer.Produce<TestEventTwo>("orders", endpoint => endpoint.ProduceTo("orders-eu"));

                    if (!topicProducerFirst)
                        producer.Produce<TestEventOne>("other", endpoint => endpoint.ProduceTo("orders"));
                }))
            .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint("orders");

        producer.EndpointConfiguration.FriendlyName.ShouldBe("orders");
        producer.EndpointConfiguration.RawName.ShouldBe("orders-eu");
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldProduceToEndpointWithFriendlyName_WhenOtherEndpointSubscribesTopicWithSameName()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume("other", endpoint => endpoint.ConsumeFrom("orders"))
                    .Consume("orders", endpoint => endpoint.ConsumeFrom("orders-eu"))))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint("orders").ProduceAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem().Endpoint.RawName.ShouldBe("orders-eu");
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldThrow_WhenEndpointNameIsTopicPattern()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom("topic-a"))))
            .AddIntegrationSpyAndSubscriber());

        Action act = () => Helper.GetProducerForEndpoint("^topic-");

        act.ShouldThrow<InvalidOperationException>().Message.ShouldStartWith("Cannot produce to '^topic-', since it is a topic pattern.");
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldThrow_WhenEndpointNameIsNull()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        Action act = () => Helper.GetProducerForEndpoint(null!);

        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldProduceToStaticPartition_WhenResolvedByTopicName()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka(kafka => kafka.WithDefaultPartitionsCount(5)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(new TopicPartition("topic1", 1), new TopicPartition(DefaultTopicName, 3)))))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint(DefaultTopicName).ProduceAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        IInboundEnvelope envelope = Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem();
        envelope.BrokerMessageIdentifier.ShouldBeOfType<KafkaOffset>().TopicPartition.ShouldBe(new TopicPartition(DefaultTopicName, 3));
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldProduceToFirstStaticPartition_WhenResolvedByFriendlyName()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka(kafka => kafka.WithDefaultPartitionsCount(5)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(
                        "static",
                        endpoint => endpoint.ConsumeFrom(new TopicPartition(DefaultTopicName, 3), new TopicPartition(DefaultTopicName, 1)))))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint("static").ProduceAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        IInboundEnvelope envelope = Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem();
        envelope.BrokerMessageIdentifier.ShouldBeOfType<KafkaOffset>().TopicPartition.ShouldBe(new TopicPartition(DefaultTopicName, 3));
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldMirrorJsonDeserializer()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).DeserializeJson())))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint(DefaultTopicName).ProduceAsync(new TestEventTwo { ContentEventTwo = "json" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.ShouldHaveSingleItem().EndpointConfiguration.Serializer.ShouldBeOfType<JsonMessageSerializer>();
        Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem().Message.ShouldBeOfType<TestEventTwo>().ContentEventTwo.ShouldBe("json");
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldMirrorNewtonsoftJsonDeserializer()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).DeserializeJsonUsingNewtonsoft())))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint(DefaultTopicName).ProduceAsync(new TestEventTwo { ContentEventTwo = "newtonsoft" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.ShouldHaveSingleItem().EndpointConfiguration.Serializer.ShouldBeOfType<NewtonsoftJsonMessageSerializer>();
        Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem().Message.ShouldBeOfType<TestEventTwo>().ContentEventTwo.ShouldBe("newtonsoft");
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldMirrorStringDeserializer()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).ConsumeStrings())))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint(DefaultTopicName).ProduceAsync(new StringMessage("text"));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.ShouldHaveSingleItem().EndpointConfiguration.Serializer.ShouldBeOfType<StringMessageSerializer>();
        Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem().Message.ShouldBeAssignableTo<StringMessage>().ShouldNotBeNull().Content.ShouldBe("text");
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldMirrorBinaryMessageDeserializer()
    {
        byte[] content = BytesUtil.GetRandomBytes();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).ConsumeBinaryMessages())))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint(DefaultTopicName).ProduceAsync(new BinaryMessage { Content = new MemoryStream(content) });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.ShouldHaveSingleItem().EndpointConfiguration.Serializer.ShouldBeOfType<BinaryMessageSerializer>();
        Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem().Message.ShouldBeAssignableTo<IBinaryMessage>().ShouldNotBeNull()
            .Content.ReadAll().ShouldBe(content);
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldMirrorRawDeserializer()
    {
        byte[] content = BytesUtil.GetRandomBytes();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).ConsumeRaw())))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint(DefaultTopicName).ProduceAsync(RawMessage.FromByteArray(content));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.ShouldHaveSingleItem().EndpointConfiguration.Serializer.ShouldBeOfType<RawMessageSerializer>();
        Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem().Message.ShouldBeAssignableTo<RawMessage>().ShouldNotBeNull()
            .Content.ReadAll().ShouldBe(content);
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldEncrypt_WhenConsumerDecryptsUsingAes()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .DecryptUsingAes(AesEncryptionKey))))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint(DefaultTopicName).ProduceAsync(new TestEventOne { ContentEventOne = "encrypted" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.ShouldHaveSingleItem().RawMessage.ShouldBeOfType<SymmetricEncryptStream>();
        Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem().Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("encrypted");
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldEncrypt_WhenConsumerUsesFixedInitializationVector()
    {
        byte[] initializationVector = BytesUtil.GetRandomBytes(16);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .DecryptUsingAes(AesEncryptionKey, initializationVector))))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint(DefaultTopicName).ProduceAsync(new TestEventOne { ContentEventOne = "encrypted" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem().Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("encrypted");
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldEncryptUsingKeyFromProvider_WhenConsumerUsesKeyProvider()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .DecryptUsingAes(keyIdentifier => keyIdentifier == null ? AesEncryptionKey : AesEncryptionKey2))))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint(DefaultTopicName).ProduceAsync(new TestEventOne { ContentEventOne = "encrypted" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.ShouldHaveSingleItem().Headers.GetValue(DefaultMessageHeaders.EncryptionKeyId).ShouldBeNull();
        Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem().Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("encrypted");
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldThrow_WhenKeyProviderThrows()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .DecryptUsingAes(keyIdentifier => keyIdentifier == "key1"
                            ? AesEncryptionKey
                            : throw new ArgumentException("Unknown key.", nameof(keyIdentifier))))))
            .AddIntegrationSpyAndSubscriber());

        Action act = () => Helper.GetProducerForEndpoint(DefaultTopicName);

        InvalidOperationException exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldStartWith(
            $"Cannot mirror the decryption settings of the consumer endpoint '{DefaultTopicName}', since its key provider doesn't return a " +
            "key for a null key identifier.");
        exception.InnerException.ShouldBeOfType<ArgumentException>();
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldThrow_WhenKeyProviderReturnsNull()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .DecryptUsingAes(keyIdentifier => keyIdentifier == "key1" ? AesEncryptionKey : null!))))
            .AddIntegrationSpyAndSubscriber());

        Action act = () => Helper.GetProducerForEndpoint(DefaultTopicName);

        InvalidOperationException exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldStartWith(
            $"Cannot mirror the decryption settings of the consumer endpoint '{DefaultTopicName}', since its key provider doesn't return a " +
            "key for a null key identifier.");
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldThrow_WhenDecryptionSettingsAreNotSupported()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .Decrypt(new CustomDecryptionSettings()))))
            .AddIntegrationSpyAndSubscriber());

        Action act = () => Helper.GetProducerForEndpoint(DefaultTopicName);

        Exception exception = act.ShouldThrow<NotSupportedException>();
        exception.Message.ShouldStartWith(
            $"Cannot mirror the decryption settings of the consumer endpoint '{DefaultTopicName}', since CustomDecryptionSettings is not " +
            "supported.");
    }

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

    [Fact]
    public async Task GetProducerForEndpoint_ShouldMirrorProtobufDeserializer()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka().AddMockedConfluentSchemaRegistry())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<ProtobufMessage>(endpoint => endpoint
                        .DeserializeProtobuf(protobuf => protobuf.ConnectToSchemaRegistry("http://e2e:4242"))
                        .ConsumeFrom(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint(DefaultTopicName).ProduceAsync(new ProtobufMessage { Number = "42" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem().Message.ShouldBeOfType<ProtobufMessage>().Number.ShouldBe("42");
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldMirrorJsonSchemaRegistryDeserializer()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka().AddMockedConfluentSchemaRegistry())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .DeserializeJsonUsingSchemaRegistry(json => json.ConnectToSchemaRegistry("http://e2e:4242"))
                        .ConsumeFrom(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint(DefaultTopicName).ProduceAsync(new TestEventOne { ContentEventOne = "json-schema" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem().Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("json-schema");
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldProduceToTopic_WhenEndpointUsesPartitionOffsetsProvider()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka(kafka => kafka.WithDefaultPartitionsCount(3)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(
                        "provider",
                        endpoint => endpoint.ConsumeFrom(
                            DefaultTopicName,
                            partitions => partitions.Select(partition => new TopicPartitionOffset(partition, Offset.Beginning))))))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint(DefaultTopicName).ProduceAsync(new TestEventOne { ContentEventOne = "by-topic" });
        await Helper.GetProducerForEndpoint("provider").ProduceAsync(new TestEventOne { ContentEventOne = "by-name" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.InboundEnvelopes.Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne)
            .ShouldBe(["by-topic", "by-name"], true);
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldProduceToFirstPartition_WhenNameIsRawNameOfMultiPartitionEndpoint()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka(kafka => kafka.WithDefaultPartitionsCount(5)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(new TopicPartition(DefaultTopicName, 1), new TopicPartition(DefaultTopicName, 3)))))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint($"{DefaultTopicName}[1],{DefaultTopicName}[3]").ProduceAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        IInboundEnvelope envelope = Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem();
        envelope.BrokerMessageIdentifier.ShouldBeOfType<KafkaOffset>().TopicPartition.ShouldBe(new TopicPartition(DefaultTopicName, 1));
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldProduceToPartition_WhenNameIsRawNameOfSinglePartitionEndpoint()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka(kafka => kafka.WithDefaultPartitionsCount(5)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(new TopicPartition(DefaultTopicName, 3)))))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint($"{DefaultTopicName}[3]").ProduceAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        IInboundEnvelope envelope = Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem();
        envelope.BrokerMessageIdentifier.ShouldBeOfType<KafkaOffset>().TopicPartition.ShouldBe(new TopicPartition(DefaultTopicName, 3));
    }

    private sealed record CustomDecryptionSettings : IDecryptionSettings
    {
        public void Validate()
        {
            // Nothing to validate
        }
    }
}
