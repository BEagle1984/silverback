// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Encryption;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public partial class ConsumerConfigurationBuilderTests
{
    [Fact]
    public void DecryptUsingAes_WithKeyAndIV_EncryptionSettingsSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        byte[] key = BytesUtil.GetRandomBytes();
        byte[] iv = BytesUtil.GetRandomBytes();

        TestConsumerConfiguration configuration = builder.DecryptUsingAes(key, iv).Build();

        configuration.Encryption.Should().BeOfType<SymmetricDecryptionSettings>();
        configuration.Encryption.As<SymmetricDecryptionSettings>().Key.Should().BeSameAs(key);
        configuration.Encryption.As<SymmetricDecryptionSettings>().InitializationVector.Should().BeSameAs(iv);
    }

    [Fact]
    public void DecryptUsingAes_WithKeyProvider_EncryptionSettingsSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        byte[]? key = BytesUtil.GetRandomBytes();
        byte[] iv = BytesUtil.GetRandomBytes();

        byte[] DecryptionKeyCallback(string? keyIdentifier)
        {
            return key;
        }

        TestConsumerConfiguration configuration = builder.DecryptUsingAes(DecryptionKeyCallback, iv).Build();

        configuration.Encryption.Should().BeOfType<SymmetricDecryptionSettings>();
        configuration.Encryption.As<SymmetricDecryptionSettings>().Key.Should().BeNull();
        configuration.Encryption.As<SymmetricDecryptionSettings>().InitializationVector.Should().BeSameAs(iv);
        configuration.Encryption.As<SymmetricDecryptionSettings>().KeyProvider.Should().NotBeNull();
        configuration.Encryption.As<SymmetricDecryptionSettings>().KeyProvider!("abc").Should().BeSameAs(key);
    }
}
