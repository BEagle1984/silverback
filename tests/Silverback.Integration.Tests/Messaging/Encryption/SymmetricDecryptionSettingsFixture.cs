// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Encryption;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Encryption;

public class SymmetricDecryptionSettingsFixture
{
    [Fact]
    public void Validate_ShouldThrow_WhenAlgorithmNameIsNull()
    {
        SymmetricDecryptionSettings settings = new()
        {
            AlgorithmName = null!
        };

        Action act = () => settings.Validate();

        act.Should().Throw<EndpointConfigurationException>().WithMessage("The algorithm name is required.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenBothKeyAndKeyProviderAreNull()
    {
        SymmetricDecryptionSettings settings = new()
        {
            Key = null,
            KeyProvider = null
        };

        Action act = () => settings.Validate();

        act.Should().Throw<EndpointConfigurationException>().WithMessage("A Key or a KeyProvider is required.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenBothKeyAndKeyProviderAreSet()
    {
        SymmetricDecryptionSettings settings = new()
        {
            Key = Array.Empty<byte>(),
            KeyProvider = _ => Array.Empty<byte>()
        };

        Action act = () => settings.Validate();

        act.Should().Throw<EndpointConfigurationException>().WithMessage("Cannot set both the Key and the KeyProvider.");
    }
}
