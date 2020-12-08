// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Inbound.ErrorHandling;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration
{
    public class ErrorPolicyChainBuilderMoveToKafkaTopicExtensionsTests
    {
        [Fact]
        public void ThenMoveToKafkaTopic_EndpointBuilder_MovePolicyCreated()
        {
            var builder = new ErrorPolicyChainBuilder();
            builder.ThenMoveToKafkaTopic(
                endpoint => endpoint
                    .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                    .ProduceTo("test-move"));
            var policy = builder.Build();

            policy.Should().BeOfType<MoveMessageErrorPolicy>();
            policy.As<MoveMessageErrorPolicy>().Endpoint.Name.Should().Be("test-move");
        }

        [Fact]
        public void ThenMoveToKafkaTopic_EndpointBuilderWithConfiguration_SkipPolicyCreatedAndConfigurationApplied()
        {
            var builder = new ErrorPolicyChainBuilder();
            builder.ThenMoveToKafkaTopic(
                endpoint => endpoint
                    .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                    .ProduceTo("test-move"),
                movePolicy => movePolicy.MaxFailedAttempts(42));
            var policy = builder.Build();

            policy.Should().BeOfType<MoveMessageErrorPolicy>();
            policy.As<MoveMessageErrorPolicy>().Endpoint.Name.Should().Be("test-move");
            policy.As<MoveMessageErrorPolicy>().MaxFailedAttemptsCount.Should().Be(42);
        }
    }
}
