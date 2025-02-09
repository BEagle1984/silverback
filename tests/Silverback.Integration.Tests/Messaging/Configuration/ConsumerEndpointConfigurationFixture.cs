// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public class ConsumerEndpointConfigurationFixture
{
    [Fact]
    public void Validate_ShouldNotThrow_WhenIsValid()
    {
        TestConsumerEndpointConfiguration configuration = GetValidConfiguration();

        Action act = configuration.Validate;

        act.ShouldNotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDeserializerIsNull()
    {
        TestConsumerEndpointConfiguration configuration = GetValidConfiguration() with { Deserializer = null! };

        Action act = configuration.Validate;

        act.ShouldThrow<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenSequenceIsNull()
    {
        TestConsumerEndpointConfiguration configuration = GetValidConfiguration() with { Sequence = null! };

        Action act = configuration.Validate;

        act.ShouldThrow<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenErrorPolicyIsNull()
    {
        TestConsumerEndpointConfiguration configuration = GetValidConfiguration() with { ErrorPolicy = null! };

        Action act = configuration.Validate;

        act.ShouldThrow<BrokerConfigurationException>();
    }

    private static TestConsumerEndpointConfiguration GetValidConfiguration() => new("test");
}
