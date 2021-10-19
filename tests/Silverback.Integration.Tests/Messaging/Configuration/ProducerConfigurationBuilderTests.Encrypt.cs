// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Encryption;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public partial class ProducerConfigurationBuilderTests
{
    [Fact]
    public void EncryptUsingAes_WithKeyAndIV_EncryptionSettingsSet()
    {
        TestProducerConfigurationBuilder<object> builder = new();

        byte[] key = BytesUtil.GetRandomBytes();
        byte[] iv = BytesUtil.GetRandomBytes();

        TestProducerConfiguration configuration = builder.EncryptUsingAes(key, iv).Build();

        configuration.Encryption.Should().BeOfType<SymmetricEncryptionSettings>();
        configuration.Encryption.As<SymmetricEncryptionSettings>().Key.Should().BeSameAs(key);
        configuration.Encryption.As<SymmetricEncryptionSettings>().InitializationVector.Should().BeSameAs(iv);
    }

    [Fact]
    public void EncryptUsingAes_WithKeyIdentifier_EncryptionSettingsSet()
    {
        TestProducerConfigurationBuilder<object> builder = new();

        byte[] key = BytesUtil.GetRandomBytes();
        byte[] iv = BytesUtil.GetRandomBytes();
        const string? keyIdentifier = "my-encryption-key-id";

        TestProducerConfiguration configuration = builder.EncryptUsingAes(key, keyIdentifier, iv).Build();

        configuration.Encryption.Should().BeOfType<SymmetricEncryptionSettings>();
        configuration.Encryption.As<SymmetricEncryptionSettings>().Key.Should().BeSameAs(key);
        configuration.Encryption.As<SymmetricEncryptionSettings>().InitializationVector.Should().BeSameAs(iv);
        configuration.Encryption.As<SymmetricEncryptionSettings>().KeyIdentifier.Should()
            .BeSameAs(keyIdentifier);
    }
}
