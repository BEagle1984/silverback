// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using NSubstitute;
using Silverback.Collections;
using Silverback.Messaging;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Outbound.EndpointResolvers;
using Silverback.Messaging.Outbound.Enrichers;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Messaging.Serialization;
using Silverback.Messaging.Validation;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging;

public class ProducerConfigurationTests
{
    [Fact]
    public void RawName_EndpointRawNameReturned()
    {
        IProducerEndpointResolver<TestProducerEndpoint> endpointResolver = Substitute.For<IProducerEndpointResolver<TestProducerEndpoint>>();
        endpointResolver.RawName.Returns("raw-name");

        TestProducerConfiguration configuration = new()
        {
            Endpoint = endpointResolver
        };

        configuration.RawName.Should().Be("raw-name");
    }

    [Fact]
    public void Equals_SameInstance_TrueReturned()
    {
        TestProducerConfiguration endpoint1 = new("test");
        TestProducerConfiguration endpoint2 = endpoint1;

        bool result = endpoint1.Equals(endpoint2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DefaultEndpoint_TrueReturned()
    {
        TestProducerConfiguration endpoint1 = TestProducerConfiguration.GetDefault();
        TestProducerConfiguration endpoint2 = TestProducerConfiguration.GetDefault();

        bool result = endpoint1.Equals(endpoint2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_SameTopic_TrueReturned()
    {
        TestProducerConfiguration endpoint1 = new("test");
        TestProducerConfiguration endpoint2 = new("test");

        bool result = endpoint1.Equals(endpoint2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_SameConfiguration_TrueReturned()
    {
        IOutboundMessageEnricher[] enrichers =
        {
            Substitute.For<IOutboundMessageEnricher>(),
            Substitute.For<IOutboundMessageEnricher>()
        };

        TestProducerConfiguration endpoint1 = new()
        {
            Chunk = new ChunkSettings
            {
                Size = 42
            },
            Encryption = new SymmetricEncryptionSettings
            {
                AlgorithmName = "ALGO"
            },
            Endpoint = new TestStaticEndpointResolver("test1"),
            Serializer = new JsonMessageSerializer<TestEventTwo>
            {
                Options = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    MaxDepth = 42
                }
            },
            Strategy = new OutboxProduceStrategy(new InMemoryOutboxSettings()),
            FriendlyName = "my-friend",
            MessageEnrichers = enrichers.AsValueReadOnlyCollection(),
            MessageValidationMode = MessageValidationMode.None
        };
        TestProducerConfiguration endpoint2 = new()
        {
            Chunk = new ChunkSettings
            {
                Size = 42
            },
            Encryption = new SymmetricEncryptionSettings
            {
                AlgorithmName = "ALGO"
            },
            Endpoint = new TestStaticEndpointResolver("test1"),
            Serializer = new JsonMessageSerializer<TestEventTwo>
            {
                Options = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    MaxDepth = 42
                }
            },
            Strategy = new OutboxProduceStrategy(new InMemoryOutboxSettings()),
            FriendlyName = "my-friend",
            MessageEnrichers = enrichers.AsValueReadOnlyCollection(),
            MessageValidationMode = MessageValidationMode.None
        };

        bool result = endpoint1.Equals(endpoint2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentTopic_FalseReturned()
    {
        TestProducerConfiguration endpoint1 = new("test1");
        TestProducerConfiguration endpoint2 = new("test2");

        bool result = endpoint1.Equals(endpoint2);

        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentConfiguration_FalseReturned()
    {
        IOutboundMessageEnricher[] enrichers =
        {
            Substitute.For<IOutboundMessageEnricher>(),
            Substitute.For<IOutboundMessageEnricher>()
        };

        TestProducerConfiguration endpoint1 = new()
        {
            Chunk = new ChunkSettings
            {
                Size = 42
            },
            Encryption = new SymmetricEncryptionSettings
            {
                AlgorithmName = "ALGO"
            },
            Endpoint = new TestDynamicEndpointResolver("test1"),
            Serializer = new JsonMessageSerializer<TestEventTwo>
            {
                Options = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    MaxDepth = 42
                }
            },
            Strategy = new OutboxProduceStrategy(new InMemoryOutboxSettings()),
            FriendlyName = "my-friend",
            MessageEnrichers = enrichers.AsValueReadOnlyCollection(),
            MessageValidationMode = MessageValidationMode.None
        };
        TestProducerConfiguration endpoint2 = new()
        {
            Chunk = new ChunkSettings
            {
                Size = 1000
            },
            Encryption = new SymmetricEncryptionSettings
            {
                AlgorithmName = "ALGO"
            },
            Endpoint = new TestDynamicEndpointResolver("test1"),
            Serializer = new JsonMessageSerializer<TestEventTwo>
            {
                Options = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    MaxDepth = 42
                }
            },
            Strategy = new OutboxProduceStrategy(new InMemoryOutboxSettings()),
            FriendlyName = "my-friend",
            MessageEnrichers = enrichers.AsValueReadOnlyCollection(),
            MessageValidationMode = MessageValidationMode.None
        };

        bool result = endpoint1.Equals(endpoint2);

        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_SameSerializerSettings_TrueReturned()
    {
        TestProducerConfiguration endpoint1 = new("topic")
        {
            Serializer = new JsonMessageSerializer<object>
            {
                Options =
                {
                    MaxDepth = 100
                }
            }
        };

        TestProducerConfiguration endpoint2 = new("topic")
        {
            Serializer = new JsonMessageSerializer<object>
            {
                Options =
                {
                    MaxDepth = 100
                }
            }
        };

        endpoint1.Equals(endpoint2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentSerializerSettings_FalseReturned()
    {
        TestProducerConfiguration endpoint1 = new("topic")
        {
            Serializer = new JsonMessageSerializer<object>
            {
                Options =
                {
                    MaxDepth = 100
                }
            }
        };

        TestProducerConfiguration endpoint2 = new("topic")
        {
            Serializer = new JsonMessageSerializer<object>
            {
                Options =
                {
                    MaxDepth = 8
                }
            }
        };

        endpoint1.Equals(endpoint2).Should().BeFalse();
    }

    [Fact]
    public void Validate_ValidEndpoint_NoExceptionThrown()
    {
        TestProducerConfiguration configuration = GetValidConfiguration();

        Action act = () => configuration.Validate();

        act.Should().NotThrow<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_NoEndpoint_ExceptionThrown()
    {
        TestProducerConfiguration configuration = GetValidConfiguration() with { Endpoint = null! };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_NoStrategy_ExceptionThrown()
    {
        TestProducerConfiguration configuration = GetValidConfiguration() with { Strategy = null! };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    private static TestProducerConfiguration GetValidConfiguration() => new("test");
}
