// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Encryption;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration
{
    public class ConsumerEndpointBuilderDecryptUsingExtensionsTests
    {
        [Fact]
        public void DecryptUsingAes_WithKeyAndIV_EncryptionSettingsSet()
        {
            var builder = new TestConsumerEndpointBuilder();

            var key = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var iv = new byte[] { 0x11, 0x12, 0x13, 0x14, 0x15 };

            TestConsumerEndpoint endpoint = builder.DecryptUsingAes(key, iv).Build();

            endpoint.Encryption.Should().BeOfType<SymmetricDecryptionSettings>();
            endpoint.Encryption.As<SymmetricDecryptionSettings>().Key.Should().BeSameAs(key);
            endpoint.Encryption.As<SymmetricDecryptionSettings>().InitializationVector.Should().BeSameAs(iv);
        }

        [Fact]
        public void DecryptUsingAes_WithKeyProvider_EncryptionSettingsSet()
        {
            var builder = new TestConsumerEndpointBuilder();

            var key = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var iv = new byte[] { 0x11, 0x12, 0x13, 0x14, 0x15 };

            byte[] DecryptionKeyCallback(string keyIdentifier)
            {
                return key;
            }

            TestConsumerEndpoint endpoint = builder.DecryptUsingAes(DecryptionKeyCallback, iv).Build();

            endpoint.Encryption.Should().BeOfType<SymmetricDecryptionSettings>();
            endpoint.Encryption.As<SymmetricDecryptionSettings>().Key.Should().BeNull();
            endpoint.Encryption.As<SymmetricDecryptionSettings>().InitializationVector.Should().BeSameAs(iv);
            endpoint.Encryption.As<SymmetricDecryptionSettings>().KeyProvider.Should().NotBeNull();
            endpoint.Encryption.As<SymmetricDecryptionSettings>().KeyProvider!("abc").Should().BeSameAs(key);
        }
    }
}
