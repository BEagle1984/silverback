// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.BinaryMessages;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Producing;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Messaging.Validation;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public partial class ProducerEndpointConfigurationBuilderFixture
{
    [Fact]
    public void Build_ShouldThrow_WhenConfigurationIsNotValid()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        Action act = () => builder.Encrypt(new SymmetricEncryptionSettings()).Build();

        act.ShouldThrow<BrokerConfigurationException>();
    }

    [Fact]
    public void Constructor_ShouldSetDisplayName()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(
            Substitute.For<IServiceProvider>(),
            "display-name");

        TestProducerEndpointConfiguration configuration = builder.Build();

        configuration.DisplayName.ShouldBe("display-name (test)");
    }

    [Fact]
    public void SerializeUsing_ShouldSetSerializer()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());
        BinaryMessageSerializer serializer = new();

        TestProducerEndpointConfiguration configuration = builder.SerializeUsing(serializer).Build();

        configuration.Serializer.ShouldBeSameAs(serializer);
    }

    [Fact]
    public void Encrypt_ShouldSetEncryptionSettings()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());
        SymmetricEncryptionSettings encryptionSettings = new()
        {
            AlgorithmName = "TripleDES",
            Key = new byte[10]
        };

        TestProducerEndpointConfiguration configuration = builder.Encrypt(encryptionSettings).Build();

        configuration.Encryption.ShouldBeSameAs(encryptionSettings);
    }

    [Fact]
    public void UseStrategy_ShouldSetStrategy()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(
            Substitute.For<IServiceProvider>(),
            "my-configuration");
        OutboxProduceStrategy strategy = new(new InMemoryOutboxSettings());

        TestProducerEndpointConfiguration configuration = builder.UseStrategy(strategy).Build();

        configuration.Strategy.ShouldBeSameAs(strategy);
    }

    [Fact]
    public void ProduceDirectly_ShouldSetStrategy()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.ProduceDirectly().Build();

        configuration.Strategy.ShouldBeOfType<DefaultProduceStrategy>();
    }

    [Fact]
    public void StoreToOutbox_ShouldSetStrategy()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(
            Substitute.For<IServiceProvider>(),
            "my-configuration");

        InMemoryOutboxSettings settings = new();
        TestProducerEndpointConfiguration configuration = builder.StoreToOutbox(settings).Build();

        OutboxProduceStrategy outboxStrategy = configuration.Strategy.ShouldBeOfType<OutboxProduceStrategy>();
        outboxStrategy.Settings.ShouldBe(settings);
    }

    [Fact]
    public void StoreToOutbox_ShouldSetStrategyUsingBuilder()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(
            Substitute.For<IServiceProvider>(),
            "my-configuration");

        TestProducerEndpointConfiguration configuration = builder
            .StoreToOutbox(outbox => outbox.UseMemory().WithName("test-outbox"))
            .Build();

        OutboxProduceStrategy outboxStrategy = configuration.Strategy.ShouldBeOfType<OutboxProduceStrategy>();
        InMemoryOutboxSettings outboxSettings = outboxStrategy.Settings.ShouldBeOfType<InMemoryOutboxSettings>();
        outboxSettings.OutboxName.ShouldBe("test-outbox");
    }

    [Fact]
    public void EnableChunking_ShouldSetChunkSettings()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.EnableChunking(42, false).Build();

        configuration.Chunk.ShouldNotBeNull();
        configuration.Chunk!.Size.ShouldBe(42);
        configuration.Chunk!.AlwaysAddHeaders.ShouldBeFalse();
    }

    [Fact]
    public void Build_ShouldSetMessageValidationModeToLogWarningByDefault()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.Build();

        configuration.MessageValidationMode.ShouldBe(MessageValidationMode.LogWarning);
    }

    [Fact]
    public void DisableMessageValidation_ShouldSetMessageValidationMode()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.DisableMessageValidation().Build();

        configuration.MessageValidationMode.ShouldBe(MessageValidationMode.None);
    }

    [Fact]
    public void ValidateMessage_ShouldSetMessageValidationMode_WhenThrowExceptionIsFalse()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.ValidateMessage(false).Build();

        configuration.MessageValidationMode.ShouldBe(MessageValidationMode.LogWarning);
    }

    [Fact]
    public void ValidateMessage_ShouldSetMessageValidationMode_WhenThrowExceptionIsTrue()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.ValidateMessage(true).Build();

        configuration.MessageValidationMode.ShouldBe(MessageValidationMode.ThrowException);
    }
}
