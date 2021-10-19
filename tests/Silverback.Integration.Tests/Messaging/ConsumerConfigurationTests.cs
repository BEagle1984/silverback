// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text.Json;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Serialization;
using Silverback.Messaging.Validation;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging;

public class ConsumerConfigurationTests
{
    [Fact]
    public void Equals_SameInstance_TrueReturned()
    {
        TestConsumerConfiguration configuration1 = new("test");
        TestConsumerConfiguration configuration2 = configuration1;

        bool result = configuration1.Equals(configuration2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_SameTopic_TrueReturned()
    {
        TestConsumerConfiguration configuration1 = new("test");
        TestConsumerConfiguration configuration2 = new("test");

        bool result = configuration1.Equals(configuration2);
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_SameConfiguration_TrueReturned()
    {
        RetryErrorPolicy errorPolicy = new();

        TestConsumerConfiguration configuration1 = new()
        {
            Batch = new BatchSettings
            {
                Size = 42
            },
            Encryption = new SymmetricDecryptionSettings
            {
                AlgorithmName = "ALGO"
            },
            Sequence = new SequenceSettings
            {
                Timeout = TimeSpan.FromSeconds(42)
            },
            Serializer = new JsonMessageSerializer<TestEventTwo>
            {
                Options = new JsonSerializerOptions
                {
                    IgnoreNullValues = true,
                    MaxDepth = 42
                }
            },
            ErrorPolicy = errorPolicy,
            FriendlyName = "my-friend",
            GroupId = "group1",
            MessageValidationMode = MessageValidationMode.None,
            ThrowIfUnhandled = false,
            NullMessageHandlingStrategy = NullMessageHandlingStrategy.Skip
        };
        TestConsumerConfiguration configuration2 = new()
        {
            Batch = new BatchSettings
            {
                Size = 42
            },
            Encryption = new SymmetricDecryptionSettings
            {
                AlgorithmName = "ALGO"
            },
            Sequence = new SequenceSettings
            {
                Timeout = TimeSpan.FromSeconds(42)
            },
            Serializer = new JsonMessageSerializer<TestEventTwo>
            {
                Options = new JsonSerializerOptions
                {
                    IgnoreNullValues = true,
                    MaxDepth = 42
                }
            },
            ErrorPolicy = errorPolicy,
            FriendlyName = "my-friend",
            GroupId = "group1",
            MessageValidationMode = MessageValidationMode.None,
            ThrowIfUnhandled = false,
            NullMessageHandlingStrategy = NullMessageHandlingStrategy.Skip
        };

        bool result = configuration1.Equals(configuration2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentTopic_FalseReturned()
    {
        TestConsumerConfiguration configuration1 = new("test1");
        TestConsumerConfiguration configuration2 = new("test2");

        bool result = configuration1.Equals(configuration2);

        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentConfiguration_FalseReturned()
    {
        RetryErrorPolicy errorPolicy = new();

        TestConsumerConfiguration configuration1 = new()
        {
            Batch = new BatchSettings
            {
                Size = 42
            },
            Encryption = new SymmetricDecryptionSettings
            {
                AlgorithmName = "ALGO"
            },
            Sequence = new SequenceSettings
            {
                Timeout = TimeSpan.FromSeconds(42)
            },
            Serializer = new JsonMessageSerializer<TestEventTwo>
            {
                Options = new JsonSerializerOptions
                {
                    IgnoreNullValues = true,
                    MaxDepth = 42
                }
            },
            ErrorPolicy = errorPolicy,
            FriendlyName = "my-friend",
            GroupId = "group1",
            MessageValidationMode = MessageValidationMode.None,
            ThrowIfUnhandled = false,
            NullMessageHandlingStrategy = NullMessageHandlingStrategy.Skip
        };
        TestConsumerConfiguration configuration2 = new()
        {
            Batch = new BatchSettings
            {
                Size = 42
            },
            Encryption = new SymmetricDecryptionSettings
            {
                AlgorithmName = "ALGO"
            },
            Sequence = new SequenceSettings
            {
                Timeout = TimeSpan.FromSeconds(42)
            },
            Serializer = new JsonMessageSerializer<TestEventTwo>
            {
                Options = new JsonSerializerOptions
                {
                    IgnoreNullValues = true,
                    MaxDepth = 1
                }
            },
            ErrorPolicy = errorPolicy,
            FriendlyName = "my-friend",
            GroupId = "group1",
            MessageValidationMode = MessageValidationMode.None,
            ThrowIfUnhandled = false,
            NullMessageHandlingStrategy = NullMessageHandlingStrategy.Skip
        };

        bool result = configuration1.Equals(configuration2);

        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_SameSerializerSettings_TrueReturned()
    {
        TestConsumerConfiguration configuration1 = new("topic")
        {
            Serializer = new JsonMessageSerializer<object>
            {
                Options =
                {
                    MaxDepth = 100
                }
            }
        };

        TestConsumerConfiguration configuration2 = new("topic")
        {
            Serializer = new JsonMessageSerializer<object>
            {
                Options =
                {
                    MaxDepth = 100
                }
            }
        };

        configuration1.Equals(configuration2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentSerializerSettings_FalseReturned()
    {
        TestConsumerConfiguration configuration1 = new("topic")
        {
            Serializer = new JsonMessageSerializer<object>
            {
                Options =
                {
                    MaxDepth = 100
                }
            }
        };

        TestConsumerConfiguration configuration2 = new("topic")
        {
            Serializer = new JsonMessageSerializer<object>
            {
                Options =
                {
                    MaxDepth = 8
                }
            }
        };

        configuration1.Equals(configuration2).Should().BeFalse();
    }

    [Fact]
    public void Validate_ValidEndpoint_NoExceptionThrown()
    {
        TestConsumerConfiguration configuration = GetValidConfiguration();

        Action act = () => configuration.Validate();

        act.Should().NotThrow<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_NoSequence_ExceptionThrown()
    {
        TestConsumerConfiguration configuration = GetValidConfiguration() with { Sequence = null! };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_NoErrorPolicy_ExceptionThrown()
    {
        TestConsumerConfiguration configuration = GetValidConfiguration() with { ErrorPolicy = null! };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    private static TestConsumerConfiguration GetValidConfiguration() => new("test");
}
