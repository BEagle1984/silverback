// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Confluent.SchemaRegistry;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class TestingHelperTests
{
    private static readonly byte[] AesEncryptionKey = BytesUtil.GetRandomBytes(32);

    private static readonly byte[] AesEncryptionKey2 = BytesUtil.GetRandomBytes(32);

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

    private sealed record CustomDecryptionSettings : IDecryptionSettings
    {
        public void Validate()
        {
            // Nothing to validate
        }
    }
}
