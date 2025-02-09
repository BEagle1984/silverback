// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Encryption;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public partial class ConsumerEndpointConfigurationBuilderFixture
{
    [Fact]
    public void DecryptUsingAes_ShouldSetEncryptionSettings_WhenKeyAndIVAreSpecified()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        byte[] key = BytesUtil.GetRandomBytes();
        byte[] iv = BytesUtil.GetRandomBytes();

        TestConsumerEndpointConfiguration configuration = builder.DecryptUsingAes(key, iv).Build();

        SymmetricDecryptionSettings encryptionSettings = configuration.Encryption.ShouldBeOfType<SymmetricDecryptionSettings>();
        encryptionSettings.Key.ShouldBeSameAs(key);
        encryptionSettings.InitializationVector.ShouldBeSameAs(iv);
    }

    [Fact]
    public void DecryptUsingAes_ShouldSetEncryptionSettings_WhenKeyProviderIsSpecified()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        byte[] key = BytesUtil.GetRandomBytes();
        byte[] iv = BytesUtil.GetRandomBytes();

        byte[] DecryptionKeyCallback(string? keyIdentifier) => key;

        TestConsumerEndpointConfiguration configuration = builder.DecryptUsingAes(DecryptionKeyCallback, iv).Build();

        SymmetricDecryptionSettings encryptionSettings = configuration.Encryption.ShouldBeOfType<SymmetricDecryptionSettings>();
        encryptionSettings.Key.ShouldBeNull();
        encryptionSettings.InitializationVector.ShouldBeSameAs(iv);
        encryptionSettings.KeyProvider.ShouldNotBeNull();
        encryptionSettings.KeyProvider!("abc").ShouldBeSameAs(key);
    }
}
