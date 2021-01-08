// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using MQTTnet.Client.Options;
using MQTTnet.Protocol;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt
{
    public class MqttConsumerEndpointBuilderTests
    {
        private readonly MqttClientConfig _clientConfig = new()
        {
            ChannelOptions = new MqttClientTcpOptions
            {
                Server = "tests-server"
            }
        };

        [Fact]
        public void Build_WithoutTopicName_ExceptionThrown()
        {
            var builder = new MqttConsumerEndpointBuilder(_clientConfig);

            Action act = () => builder.Build();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void Build_WithoutServer_ExceptionThrown()
        {
            var builder = new MqttConsumerEndpointBuilder(new MqttClientConfig());

            Action act = () =>
            {
                builder.ConsumeFrom("some-topic");
                builder.Build();
            };

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void ConsumeFrom_SingleTopic_TopicSet()
        {
            var builder = new MqttConsumerEndpointBuilder(_clientConfig);
            builder.ConsumeFrom("some-topic");
            var endpoint = builder.Build();

            endpoint.Name.Should().Be("some-topic");
        }

        [Fact]
        public void ConsumeFrom_MultipleTopics_TopicsSet()
        {
            var builder = new MqttConsumerEndpointBuilder(_clientConfig);
            builder.ConsumeFrom("some-topic", "some-other-topic");
            var endpoint = builder.Build();

            endpoint.Topics.Should().BeEquivalentTo("some-topic", "some-other-topic");
        }

        [Fact]
        public void WithQualityOfServiceLevel_QualityOfServiceLevelSet()
        {
            var builder = new MqttConsumerEndpointBuilder(_clientConfig);
            builder
                .ConsumeFrom("some-topic")
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);
            var endpoint = builder.Build();

            endpoint.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtLeastOnce);
        }

        [Fact]
        public void WithAtMostOnceQoS_QualityOfServiceLevelSet()
        {
            var builder = new MqttConsumerEndpointBuilder(_clientConfig);
            builder
                .ConsumeFrom("some-topic")
                .WithAtMostOnceQoS();
            var endpoint = builder.Build();

            endpoint.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtMostOnce);
        }

        [Fact]
        public void WithAtLeastOnceQoS_QualityOfServiceLevelSet()
        {
            var builder = new MqttConsumerEndpointBuilder(_clientConfig);
            builder
                .ConsumeFrom("some-topic")
                .WithAtLeastOnceQoS();
            var endpoint = builder.Build();

            endpoint.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtLeastOnce);
        }

        [Fact]
        public void WithExactlyOnceQoS_QualityOfServiceLevelSet()
        {
            var builder = new MqttConsumerEndpointBuilder(_clientConfig);
            builder
                .ConsumeFrom("some-topic")
                .WithExactlyOnceQoS();
            var endpoint = builder.Build();

            endpoint.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.ExactlyOnce);
        }
    }
}
