// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Encryption;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Encryption;

public class SymmetricEncryptionSettingsFixture
{
    [Fact]
    public void Validate_ShouldThrow_WhenAlgorithmNameIsNull()
    {
        SymmetricEncryptionSettings settings = new()
        {
            AlgorithmName = null!
        };

        Action act = () => settings.Validate();

        act.Should().Throw<EndpointConfigurationException>().WithMessage("The algorithm name is required.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenKeyIsNull()
    {
        SymmetricEncryptionSettings settings = new()
        {
            Key = null
        };

        Action act = () => settings.Validate();

        act.Should().Throw<EndpointConfigurationException>().WithMessage("A Key is required.");
    }
}
