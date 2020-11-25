// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Inbound.ExactlyOnce;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration
{
    public class ConsumerEndpointBuilderTests
    {
        [Fact]
        public void Build_InvalidConfiguration_ExceptionThrown()
        {
            var builder = new TestConsumerEndpointBuilder();

            Action act = () => builder.Decrypt(new SymmetricEncryptionSettings()).Build();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void DeserializeUsing_Serializer_SerializerSet()
        {
            var builder = new TestConsumerEndpointBuilder();
            var serializer = new BinaryFileMessageSerializer();

            var endpoint = builder.DeserializeUsing(serializer).Build();

            endpoint.Serializer.Should().BeSameAs(serializer);
        }

        [Fact]
        public void Decrypt_EncryptionSettings_EncryptionSet()
        {
            var builder = new TestConsumerEndpointBuilder();
            var encryptionSettings = new SymmetricEncryptionSettings
            {
                AlgorithmName = "TripleDES",
                Key = new byte[10]
            };

            var endpoint = builder.Decrypt(encryptionSettings).Build();

            endpoint.Encryption.Should().BeSameAs(encryptionSettings);
        }

        [Fact]
        public void OnError_ErrorPolicy_ErrorPolicySet()
        {
            var builder = new TestConsumerEndpointBuilder();
            var errorPolicy = new RetryErrorPolicy();

            var endpoint = builder.OnError(errorPolicy).Build();

            endpoint.ErrorPolicy.Should().BeSameAs(errorPolicy);
        }

        [Fact]
        public void OnError_ErrorPolicyBuildAction_ErrorPolicySet()
        {
            var builder = new TestConsumerEndpointBuilder();

            var endpoint = builder.OnError(errorPolicy => errorPolicy.Retry(5).ThenSkip()).Build();

            endpoint.ErrorPolicy.Should().BeOfType<ErrorPolicyChain>();
        }

        [Fact]
        public void EnsureExactlyOnce_Strategy_ExactlyOnceStrategySet()
        {
            var builder = new TestConsumerEndpointBuilder();
            var strategy = new OffsetStoreExactlyOnceStrategy();

            var endpoint = builder.EnsureExactlyOnce(strategy).Build();

            endpoint.ExactlyOnceStrategy.Should().BeSameAs(strategy);
        }

        [Fact]
        public void EnsureExactlyOnce_StrategyBuildAction_ExactlyOnceStrategySet()
        {
            var builder = new TestConsumerEndpointBuilder();

            var endpoint = builder.EnsureExactlyOnce(strategy => strategy.StoreOffsets()).Build();

            endpoint.ExactlyOnceStrategy.Should().BeOfType<OffsetStoreExactlyOnceStrategy>();
        }

        [Fact]
        public void EnableBatchProcessing_ValidBatchSettings_BatchSettingsSet()
        {
            var builder = new TestConsumerEndpointBuilder();

            var endpoint = builder.EnableBatchProcessing(42, TimeSpan.FromMinutes(42)).Build();

            endpoint.Batch.Should().NotBeNull();
            endpoint.Batch!.Size.Should().Be(42);
            endpoint.Batch!.MaxWaitTime.Should().Be(TimeSpan.FromMinutes(42));
        }

        [Fact]
        public void EnableBatchProcessing_InvalidBatchSize_ExceptionThrown()
        {
            var builder = new TestConsumerEndpointBuilder();

            Action act = () => builder.EnableBatchProcessing(1).Build();

            act.Should().ThrowExactly<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void WithSequenceTimeout_Timeout_SequenceTimeoutSet()
        {
            var builder = new TestConsumerEndpointBuilder();

            var endpoint = builder.WithSequenceTimeout(TimeSpan.FromMinutes(42)).Build();

            endpoint.Sequence.Timeout.Should().Be(TimeSpan.FromMinutes(42));
        }

        [Fact]
        public void ThrowIfUnhandled_ThrowIfUnhandledSet()
        {
            var builder = new TestConsumerEndpointBuilder();

            var endpoint = builder.ThrowIfUnhandled().Build();

            endpoint.ThrowIfUnhandled.Should().Be(true);
        }

        [Fact]
        public void IgnoreUnhandledMessages_ThrowIfUnhandledSet()
        {
            var builder = new TestConsumerEndpointBuilder();

            var endpoint = builder.IgnoreUnhandledMessages().Build();

            endpoint.ThrowIfUnhandled.Should().Be(false);
        }
    }
}
