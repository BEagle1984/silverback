// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Encryption;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public partial class ProducerEndpointConfigurationBuilderFixture
{
    [Fact]
    public void EncryptUsingAes_ShouldSetKeyAndIV()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        byte[] key = BytesUtil.GetRandomBytes();
        byte[] iv = BytesUtil.GetRandomBytes();

        TestProducerEndpointConfiguration configuration = builder.EncryptUsingAes(key, iv).Build();

        SymmetricEncryptionSettings encryptionSettings = configuration.Encryption.ShouldBeOfType<SymmetricEncryptionSettings>();
        encryptionSettings.Key.ShouldBeSameAs(key);
        encryptionSettings.InitializationVector.ShouldBeSameAs(iv);
    }

    [Fact]
    public void EncryptUsingAes_ShouldSetKeyIdentifier()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        byte[] key = BytesUtil.GetRandomBytes();
        byte[] iv = BytesUtil.GetRandomBytes();
        const string? keyIdentifier = "my-encryption-key-id";

        TestProducerEndpointConfiguration configuration = builder.EncryptUsingAes(key, keyIdentifier, iv).Build();

        SymmetricEncryptionSettings encryptionSettings = configuration.Encryption.ShouldBeOfType<SymmetricEncryptionSettings>();
        encryptionSettings.Key.ShouldBeSameAs(key);
        encryptionSettings.InitializationVector.ShouldBeSameAs(iv);
        encryptionSettings.KeyIdentifier.ShouldBe(keyIdentifier);
    }
}
