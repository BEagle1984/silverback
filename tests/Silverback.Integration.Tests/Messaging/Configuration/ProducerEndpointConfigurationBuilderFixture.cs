// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using NSubstitute;
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

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void Constructor_ShouldSetDisplayName()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(
            Substitute.For<IServiceProvider>(),
            "display-name");

        TestProducerEndpointConfiguration configuration = builder.Build();

        configuration.DisplayName.Should().Be("display-name (test)");
    }

    [Fact]
    public void SerializeUsing_ShouldSetSerializer()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());
        BinaryMessageSerializer serializer = new();

        TestProducerEndpointConfiguration configuration = builder.SerializeUsing(serializer).Build();

        configuration.Serializer.Should().BeSameAs(serializer);
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

        configuration.Encryption.Should().BeSameAs(encryptionSettings);
    }

    [Fact]
    public void UseStrategy_ShouldSetStrategy()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(
            Substitute.For<IServiceProvider>(),
            "my-configuration");
        OutboxProduceStrategy strategy = new(new InMemoryOutboxSettings());

        TestProducerEndpointConfiguration configuration = builder.UseStrategy(strategy).Build();

        configuration.Strategy.Should().BeSameAs(strategy);
    }

    [Fact]
    public void ProduceDirectly_ShouldSetStrategy()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.ProduceDirectly().Build();

        configuration.Strategy.Should().BeOfType<DefaultProduceStrategy>();
    }

    [Fact]
    public void StoreToOutbox_ShouldSetStrategy()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(
            Substitute.For<IServiceProvider>(),
            "my-configuration");

        InMemoryOutboxSettings settings = new();
        TestProducerEndpointConfiguration configuration = builder.StoreToOutbox(settings).Build();

        configuration.Strategy.Should().BeOfType<OutboxProduceStrategy>();
        configuration.Strategy.As<OutboxProduceStrategy>().Settings.Should().Be(settings);
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

        configuration.Strategy.Should().BeOfType<OutboxProduceStrategy>();
        configuration.Strategy.As<OutboxProduceStrategy>().Settings.As<InMemoryOutboxSettings>().OutboxName.Should().Be("test-outbox");
    }

    [Fact]
    public void EnableChunking_ShouldSetChunkSettings()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.EnableChunking(42, false).Build();

        configuration.Chunk.Should().NotBeNull();
        configuration.Chunk!.Size.Should().Be(42);
        configuration.Chunk!.AlwaysAddHeaders.Should().BeFalse();
    }

    [Fact]
    public void Build_ShouldSetMessageValidationModeToLogWarningByDefault()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.Build();

        configuration.MessageValidationMode.Should().Be(MessageValidationMode.LogWarning);
    }

    [Fact]
    public void DisableMessageValidation_ShouldSetMessageValidationMode()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.DisableMessageValidation().Build();

        configuration.MessageValidationMode.Should().Be(MessageValidationMode.None);
    }

    [Fact]
    public void ValidateMessage_ShouldSetMessageValidationMode_WhenThrowExceptionIsFalse()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.ValidateMessage(false).Build();

        configuration.MessageValidationMode.Should().Be(MessageValidationMode.LogWarning);
    }

    [Fact]
    public void ValidateMessage_ShouldSetMessageValidationMode_WhenThrowExceptionIsTrue()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.ValidateMessage(true).Build();

        configuration.MessageValidationMode.Should().Be(MessageValidationMode.ThrowException);
    }
}
