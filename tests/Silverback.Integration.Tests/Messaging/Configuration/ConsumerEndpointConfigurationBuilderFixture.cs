// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.BinaryMessages;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Validation;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public partial class ConsumerEndpointConfigurationBuilderFixture
{
    [Fact]
    public void Build_ShouldThrow_WhenConfigurationIsNotValid()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        Action act = () => builder.Decrypt(new SymmetricDecryptionSettings()).Build();

        act.ShouldThrow<BrokerConfigurationException>();
    }

    [Fact]
    public void Constructor_ShouldSetDisplayName()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(
            Substitute.For<IServiceProvider>(),
            "display-name");

        TestConsumerEndpointConfiguration endpoint = builder.Build();

        endpoint.DisplayName.ShouldBe("display-name (test)");
    }

    [Fact]
    public void DeserializeUsing_ShouldSetSerializer()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());
        BinaryMessageDeserializer<BinaryMessage> deserializer = new();

        TestConsumerEndpointConfiguration endpoint = builder.DeserializeUsing(deserializer).Build();

        endpoint.Deserializer.ShouldBeSameAs(deserializer);
    }

    [Fact]
    public void Decrypt_ShouldSetEncryptionSettings()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());
        SymmetricDecryptionSettings encryptionSettings = new()
        {
            AlgorithmName = "TripleDES",
            Key = new byte[10]
        };

        TestConsumerEndpointConfiguration endpoint = builder.Decrypt(encryptionSettings).Build();

        endpoint.Encryption.ShouldBeSameAs(encryptionSettings);
    }

    [Fact]
    public void OnError_ShouldSetErrorPolicy()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());
        RetryErrorPolicy errorPolicy = new();

        TestConsumerEndpointConfiguration endpoint = builder.OnError(errorPolicy).Build();

        endpoint.ErrorPolicy.ShouldBeSameAs(errorPolicy);
    }

    [Fact]
    public void OnError_ShouldSetErrorPolicyViaBuilder()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration endpoint = builder.OnError(errorPolicy => errorPolicy.Retry(5).ThenSkip()).Build();

        endpoint.ErrorPolicy.ShouldBeOfType<ErrorPolicyChain>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2000)]
    public void EnableBatchProcessing_ShouldSetBatchSettings(int size)
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration endpoint = builder.EnableBatchProcessing(size, TimeSpan.FromMinutes(42)).Build();

        endpoint.Batch.ShouldNotBeNull();
        endpoint.Batch!.Size.ShouldBe(size);
        endpoint.Batch!.MaxWaitTime.ShouldBe(TimeSpan.FromMinutes(42));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void EnableBatchProcessing_ShouldThrow_WhenBatchSizeIsOutOfRange(int size)
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        Action act = () => builder.EnableBatchProcessing(size).Build();

        act.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WithSequenceTimeout_ShouldSetSequenceTimeout()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration endpoint = builder.WithSequenceTimeout(TimeSpan.FromMinutes(42)).Build();

        endpoint.Sequence.Timeout.ShouldBe(TimeSpan.FromMinutes(42));
    }

    [Fact]
    public void ThrowIfUnhandled_ShouldSetThrowIfUnhandled()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration endpoint = builder.ThrowIfUnhandled().Build();

        endpoint.ThrowIfUnhandled.ShouldBe(true);
    }

    [Fact]
    public void IgnoreUnhandledMessages_ShouldSetThrowIfUnhandled()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration endpoint = builder.IgnoreUnhandledMessages().Build();

        endpoint.ThrowIfUnhandled.ShouldBe(false);
    }

    [Fact]
    public void Build_ShouldSetMessageValidationModeToLogWarningByDefault()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration endpoint = builder.Build();

        endpoint.MessageValidationMode.ShouldBe(MessageValidationMode.LogWarning);
    }

    [Fact]
    public void DisableMessageValidation_ShouldSetMessageValidationMode()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration endpoint = builder.DisableMessageValidation().Build();

        endpoint.MessageValidationMode.ShouldBe(MessageValidationMode.None);
    }

    [Fact]
    public void ValidateMessage_ShouldSetMessageValidationMode_WhenThrowExceptionIsFalse()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration endpoint = builder.ValidateMessage(false).Build();

        endpoint.MessageValidationMode.ShouldBe(MessageValidationMode.LogWarning);
    }

    [Fact]
    public void ValidateMessage_ShouldSetMessageValidationMode_WhenThrowExceptionIsTrue()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration endpoint = builder.ValidateMessage(true).Build();

        endpoint.MessageValidationMode.ShouldBe(MessageValidationMode.ThrowException);
    }
}
