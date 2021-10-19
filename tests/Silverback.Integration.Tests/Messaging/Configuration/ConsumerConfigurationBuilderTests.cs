// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Inbound.ExactlyOnce;
using Silverback.Messaging.Serialization;
using Silverback.Messaging.Validation;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public partial class ConsumerConfigurationBuilderTests
{
    [Fact]
    public void Build_InvalidConfiguration_ExceptionThrown()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        Action act = () => builder.Decrypt(new SymmetricDecryptionSettings()).Build();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void WithName_DisplayNameSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder.WithName("display-name").Build();

        endpoint.DisplayName.Should().Be("display-name (test)");
    }

    [Fact]
    public void DeserializeUsing_Serializer_SerializerSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();
        BinaryFileMessageSerializer serializer = new();

        TestConsumerConfiguration endpoint = builder.DeserializeUsing(serializer).Build();

        endpoint.Serializer.Should().BeSameAs(serializer);
    }

    [Fact]
    public void Decrypt_EncryptionSettings_EncryptionSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();
        SymmetricDecryptionSettings encryptionSettings = new()
        {
            AlgorithmName = "TripleDES",
            Key = new byte[10]
        };

        TestConsumerConfiguration endpoint = builder.Decrypt(encryptionSettings).Build();

        endpoint.Encryption.Should().BeSameAs(encryptionSettings);
    }

    [Fact]
    public void OnError_ErrorPolicy_ErrorPolicySet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();
        RetryErrorPolicy errorPolicy = new();

        TestConsumerConfiguration endpoint = builder.OnError(errorPolicy).Build();

        endpoint.ErrorPolicy.Should().BeSameAs(errorPolicy);
    }

    [Fact]
    public void OnError_ErrorPolicyBuildAction_ErrorPolicySet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder.OnError(errorPolicy => errorPolicy.Retry(5).ThenSkip()).Build();

        endpoint.ErrorPolicy.Should().BeOfType<ErrorPolicyChain>();
    }

    [Fact]
    public void EnsureExactlyOnce_Strategy_ExactlyOnceStrategySet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();
        OffsetStoreExactlyOnceStrategy strategy = new();

        TestConsumerConfiguration endpoint = builder.EnsureExactlyOnce(strategy).Build();

        endpoint.ExactlyOnceStrategy.Should().BeSameAs(strategy);
    }

    [Fact]
    public void EnsureExactlyOnce_StrategyBuildAction_ExactlyOnceStrategySet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder.EnsureExactlyOnce(strategy => strategy.StoreOffsets()).Build();

        endpoint.ExactlyOnceStrategy.Should().BeOfType<OffsetStoreExactlyOnceStrategy>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2000)]
    public void EnableBatchProcessing_ValidBatchSettings_BatchSettingsSet(int size)
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder.EnableBatchProcessing(size, TimeSpan.FromMinutes(42)).Build();

        endpoint.Batch.Should().NotBeNull();
        endpoint.Batch!.Size.Should().Be(size);
        endpoint.Batch!.MaxWaitTime.Should().Be(TimeSpan.FromMinutes(42));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void EnableBatchProcessing_InvalidBatchSize_ExceptionThrown(int size)
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        Action act = () => builder.EnableBatchProcessing(size).Build();

        act.Should().ThrowExactly<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WithSequenceTimeout_Timeout_SequenceTimeoutSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder.WithSequenceTimeout(TimeSpan.FromMinutes(42)).Build();

        endpoint.Sequence.Timeout.Should().Be(TimeSpan.FromMinutes(42));
    }

    [Fact]
    public void ThrowIfUnhandled_ThrowIfUnhandledSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder.ThrowIfUnhandled().Build();

        endpoint.ThrowIfUnhandled.Should().Be(true);
    }

    [Fact]
    public void IgnoreUnhandledMessages_ThrowIfUnhandledSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder.IgnoreUnhandledMessages().Build();

        endpoint.ThrowIfUnhandled.Should().Be(false);
    }

    [Fact]
    public void HandleTombstoneMessages_NullMessageHandlingSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder.HandleTombstoneMessages().Build();

        endpoint.NullMessageHandlingStrategy.Should().Be(NullMessageHandlingStrategy.Tombstone);
    }

    [Fact]
    public void SkipNullMessages_NullMessageHandlingSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder.SkipNullMessages().Build();

        endpoint.NullMessageHandlingStrategy.Should().Be(NullMessageHandlingStrategy.Skip);
    }

    [Fact]
    public void UseLegacyNullMessageHandling_NullMessageHandlingSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder.UseLegacyNullMessageHandling().Build();

        endpoint.NullMessageHandlingStrategy.Should().Be(NullMessageHandlingStrategy.Legacy);
    }

    [Fact]
    public void MessageValidationMode_ByDefault_IsLogWarning()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder.Build();

        endpoint.MessageValidationMode.Should().Be(MessageValidationMode.LogWarning);
    }

    [Fact]
    public void DisableMessageValidation_MessageValidationMode_IsNone()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder.DisableMessageValidation().Build();

        endpoint.MessageValidationMode.Should().Be(MessageValidationMode.None);
    }

    [Fact]
    public void ValidateMessage_NoThrowException_MessageValidationModeIsLogWarning()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder.ValidateMessage(false).Build();

        endpoint.MessageValidationMode.Should().Be(MessageValidationMode.LogWarning);
    }

    [Fact]
    public void ValidateMessage_ThrowException_MessageValidationModeIsThrowException()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder.ValidateMessage(true).Build();

        endpoint.MessageValidationMode.Should().Be(MessageValidationMode.ThrowException);
    }
}
