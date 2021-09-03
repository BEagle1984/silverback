// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Encryption;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration
{
    public class ProducerEndpointBuilderEncryptUsingExtensionsTests
    {
        [Fact]
        public void EncryptUsingAes_WithKeyAndIV_EncryptionSettingsSet()
        {
            var builder = new TestProducerEndpointBuilder();

            var key = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var iv = new byte[] { 0x11, 0x12, 0x13, 0x14, 0x15 };

            TestProducerEndpoint endpoint = builder.EncryptUsingAes(key, iv).Build();

            endpoint.Encryption.Should().BeOfType<SymmetricEncryptionSettings>();
            endpoint.Encryption.As<SymmetricEncryptionSettings>().Key.Should().BeSameAs(key);
            endpoint.Encryption.As<SymmetricEncryptionSettings>().InitializationVector.Should().BeSameAs(iv);
        }

        [Fact]
        public void EncryptUsingAes_WithKeyIdentifier_EncryptionSettingsSet()
        {
            var builder = new TestProducerEndpointBuilder();

            var key = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var iv = new byte[] { 0x11, 0x12, 0x13, 0x14, 0x15 };
            const string? keyIdentifier = "my-encryption-key-id";

            TestProducerEndpoint endpoint = builder.EncryptUsingAes(key, keyIdentifier, iv).Build();

            endpoint.Encryption.Should().BeOfType<SymmetricEncryptionSettings>();
            endpoint.Encryption.As<SymmetricEncryptionSettings>().Key.Should().BeSameAs(key);
            endpoint.Encryption.As<SymmetricEncryptionSettings>().InitializationVector.Should().BeSameAs(iv);
            endpoint.Encryption.As<SymmetricEncryptionSettings>().KeyIdentifier.Should()
                .BeSameAs(keyIdentifier);
        }
    }
}
