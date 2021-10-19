// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Outbound;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Messaging.Validation;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public partial class ProducerConfigurationBuilderTests
{
    [Fact]
    public void Build_InvalidConfiguration_ExceptionThrown()
    {
        TestProducerConfigurationBuilder<object> builder = new();

        Action act = () => builder.Encrypt(new SymmetricEncryptionSettings()).Build();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void WithName_DisplayNameSet()
    {
        TestProducerConfigurationBuilder<object> builder = new();

        TestProducerConfiguration endpoint = builder.WithName("display-name").Build();

        endpoint.DisplayName.Should().Be("display-name (test)");
    }

    [Fact]
    public void SerializeUsing_Serializer_SerializerSet()
    {
        TestProducerConfigurationBuilder<object> builder = new();
        BinaryFileMessageSerializer serializer = new();

        TestProducerConfiguration endpoint = builder.SerializeUsing(serializer).Build();

        endpoint.Serializer.Should().BeSameAs(serializer);
    }

    [Fact]
    public void Encrypt_EncryptionSettings_EncryptionSet()
    {
        TestProducerConfigurationBuilder<object> builder = new();
        SymmetricEncryptionSettings encryptionSettings = new()
        {
            AlgorithmName = "TripleDES",
            Key = new byte[10]
        };

        TestProducerConfiguration endpoint = builder.Encrypt(encryptionSettings).Build();

        endpoint.Encryption.Should().BeSameAs(encryptionSettings);
    }

    [Fact]
    public void UseStrategy_Strategy_StrategySet()
    {
        TestProducerConfigurationBuilder<object> builder = new();
        OutboxProduceStrategy strategy = new();

        TestProducerConfiguration endpoint = builder.UseStrategy(strategy).Build();

        endpoint.Strategy.Should().BeSameAs(strategy);
    }

    [Fact]
    public void ProduceDirectly_StrategySet()
    {
        TestProducerConfigurationBuilder<object> builder = new();

        TestProducerConfiguration endpoint = builder.ProduceDirectly().Build();

        endpoint.Strategy.Should().BeOfType<DefaultProduceStrategy>();
    }

    [Fact]
    public void ProduceToOutbox_StrategySet()
    {
        TestProducerConfigurationBuilder<object> builder = new();

        TestProducerConfiguration endpoint = builder.ProduceToOutbox().Build();

        endpoint.Strategy.Should().BeOfType<OutboxProduceStrategy>();
    }

    [Fact]
    public void EnableChunking_ValidBatchSettings_BatchSettingsSet()
    {
        TestProducerConfigurationBuilder<object> builder = new();

        TestProducerConfiguration endpoint = builder.EnableChunking(42, false).Build();

        endpoint.Chunk.Should().NotBeNull();
        endpoint.Chunk!.Size.Should().Be(42);
        endpoint.Chunk!.AlwaysAddHeaders.Should().BeFalse();
    }

    [Fact]
    public void MessageValidationMode_ByDefault_IsLogWarning()
    {
        TestProducerConfigurationBuilder<object> builder = new();

        TestProducerConfiguration endpoint = builder.Build();

        endpoint.MessageValidationMode.Should().Be(MessageValidationMode.LogWarning);
    }

    [Fact]
    public void DisableMessageValidation_MessageValidationMode_IsNone()
    {
        TestProducerConfigurationBuilder<object> builder = new();

        TestProducerConfiguration endpoint = builder.DisableMessageValidation().Build();

        endpoint.MessageValidationMode.Should().Be(MessageValidationMode.None);
    }

    [Fact]
    public void ValidateMessage_NoThrowException_MessageValidationModeIsLogWarning()
    {
        TestProducerConfigurationBuilder<object> builder = new();

        TestProducerConfiguration endpoint = builder.ValidateMessage(false).Build();

        endpoint.MessageValidationMode.Should().Be(MessageValidationMode.LogWarning);
    }

    [Fact]
    public void ValidateMessage_ThrowException_MessageValidationModeIsThrowException()
    {
        TestProducerConfigurationBuilder<object> builder = new();

        TestProducerConfiguration endpoint = builder.ValidateMessage(true).Build();

        endpoint.MessageValidationMode.Should().Be(MessageValidationMode.ThrowException);
    }
}
