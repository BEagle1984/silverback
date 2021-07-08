// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Outbound;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration
{
    public class ProducerEndpointBuilderTests
    {
        [Fact]
        public void Build_InvalidConfiguration_ExceptionThrown()
        {
            var builder = new TestProducerEndpointBuilder();

            Action act = () => builder.Encrypt(new SymmetricEncryptionSettings()).Build();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void WithName_DisplayNameSet()
        {
            var builder = new TestProducerEndpointBuilder();

            var endpoint = builder.WithName("display-name").Build();

            endpoint.DisplayName.Should().Be("display-name [test]");
        }

        [Fact]
        public void SerializeUsing_Serializer_SerializerSet()
        {
            var builder = new TestProducerEndpointBuilder();
            var serializer = new BinaryFileMessageSerializer();

            var endpoint = builder.SerializeUsing(serializer).Build();

            endpoint.Serializer.Should().BeSameAs(serializer);
        }

        [Fact]
        public void Encrypt_EncryptionSettings_EncryptionSet()
        {
            var builder = new TestProducerEndpointBuilder();
            var encryptionSettings = new SymmetricEncryptionSettings
            {
                AlgorithmName = "TripleDES",
                Key = new byte[10]
            };

            var endpoint = builder.Encrypt(encryptionSettings).Build();

            endpoint.Encryption.Should().BeSameAs(encryptionSettings);
        }

        [Fact]
        public void UseStrategy_Strategy_StrategySet()
        {
            var builder = new TestProducerEndpointBuilder();
            var strategy = new OutboxProduceStrategy();

            var endpoint = builder.UseStrategy(strategy).Build();

            endpoint.Strategy.Should().BeSameAs(strategy);
        }

        [Fact]
        public void ProduceDirectly_StrategySet()
        {
            var builder = new TestProducerEndpointBuilder();

            var endpoint = builder.ProduceDirectly().Build();

            endpoint.Strategy.Should().BeOfType<DefaultProduceStrategy>();
        }

        [Fact]
        public void ProduceToOutbox_StrategySet()
        {
            var builder = new TestProducerEndpointBuilder();

            var endpoint = builder.ProduceToOutbox().Build();

            endpoint.Strategy.Should().BeOfType<OutboxProduceStrategy>();
        }

        [Fact]
        public void EnableChunking_ValidBatchSettings_BatchSettingsSet()
        {
            var builder = new TestProducerEndpointBuilder();

            var endpoint = builder.EnableChunking(42, false).Build();

            endpoint.Chunk.Should().NotBeNull();
            endpoint.Chunk!.Size.Should().Be(42);
            endpoint.Chunk!.AlwaysAddHeaders.Should().BeFalse();
        }
    }
}
