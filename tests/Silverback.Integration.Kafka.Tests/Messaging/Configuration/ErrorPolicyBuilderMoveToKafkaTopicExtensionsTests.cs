// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Inbound.ErrorHandling;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration
{
    public class ErrorPolicyBuilderMoveToKafkaTopicExtensionsTests
    {
        private readonly IKafkaEndpointsConfigurationBuilder _endpointsConfigurationBuilder;

        public ErrorPolicyBuilderMoveToKafkaTopicExtensionsTests()
        {
            _endpointsConfigurationBuilder = new KafkaEndpointsConfigurationBuilder(
                new EndpointsConfigurationBuilder(Substitute.For<IServiceProvider>()));

            _endpointsConfigurationBuilder.Configure(config => { config.BootstrapServers = "PLAINTEXT://tests"; });
        }

        [Fact]
        public void MoveToKafkaTopic_EndpointBuilder_MovePolicyCreated()
        {
            var builder = new ErrorPolicyBuilder(_endpointsConfigurationBuilder);
            builder.MoveToKafkaTopic(endpoint => endpoint.ProduceTo("test-move"));
            var policy = builder.Build();

            policy.Should().BeOfType<MoveMessageErrorPolicy>();
            policy.As<MoveMessageErrorPolicy>().Endpoint.Name.Should().Be("test-move");
            policy.As<MoveMessageErrorPolicy>().Endpoint.As<KafkaProducerEndpoint>().Configuration.BootstrapServers
                .Should().Be("PLAINTEXT://tests");
        }

        [Fact]
        public void MoveToKafkaTopic_EndpointBuilderWithConfiguration_SkipPolicyCreatedAndConfigurationApplied()
        {
            var builder = new ErrorPolicyBuilder(_endpointsConfigurationBuilder);
            builder.MoveToKafkaTopic(
                endpoint => endpoint.ProduceTo("test-move"),
                movePolicy => movePolicy.MaxFailedAttempts(42));
            var policy = builder.Build();

            policy.Should().BeOfType<MoveMessageErrorPolicy>();
            policy.As<MoveMessageErrorPolicy>().Endpoint.Name.Should().Be("test-move");
            policy.As<MoveMessageErrorPolicy>().MaxFailedAttemptsCount.Should().Be(42);
            policy.As<MoveMessageErrorPolicy>().Endpoint.As<KafkaProducerEndpoint>().Configuration.BootstrapServers
                .Should().Be("PLAINTEXT://tests");
        }
    }
}
