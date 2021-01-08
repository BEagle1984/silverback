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
    public class MqttProducerEndpointBuilderTests
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
            var builder = new MqttProducerEndpointBuilder(_clientConfig);

            Action act = () => builder.Build();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void Build_WithoutServer_ExceptionThrown()
        {
            var builder = new MqttProducerEndpointBuilder(new MqttClientConfig());

            Action act = () =>
            {
                builder.ProduceTo("some-topic");
                builder.Build();
            };

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void ProduceTo_TopicName_TopicSet()
        {
            var builder = new MqttProducerEndpointBuilder(_clientConfig);
            builder.ProduceTo("some-topic");
            var endpoint = builder.Build();

            endpoint.Name.Should().Be("some-topic");
        }

        [Fact]
        public void WithQualityOfServiceLevel_QualityOfServiceLevelSet()
        {
            var builder = new MqttProducerEndpointBuilder(_clientConfig);
            builder
                .ProduceTo("some-topic")
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);
            var endpoint = builder.Build();

            endpoint.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtLeastOnce);
        }

        [Fact]
        public void WithAtMostOnceQoS_QualityOfServiceLevelSet()
        {
            var builder = new MqttProducerEndpointBuilder(_clientConfig);
            builder
                .ProduceTo("some-topic")
                .WithAtMostOnceQoS();
            var endpoint = builder.Build();

            endpoint.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtMostOnce);
        }

        [Fact]
        public void WithAtLeastOnceQoS_QualityOfServiceLevelSet()
        {
            var builder = new MqttProducerEndpointBuilder(_clientConfig);
            builder
                .ProduceTo("some-topic")
                .WithAtLeastOnceQoS();
            var endpoint = builder.Build();

            endpoint.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtLeastOnce);
        }

        [Fact]
        public void Retain_RetainSet()
        {
            var builder = new MqttProducerEndpointBuilder(_clientConfig);
            builder
                .ProduceTo("some-topic")
                .Retain();
            var endpoint = builder.Build();

            endpoint.Retain.Should().BeTrue();
        }

        [Fact]
        public void WithMessageExpiration_MessageExpiryIntervalSet()
        {
            var builder = new MqttProducerEndpointBuilder(_clientConfig);
            builder
                .ProduceTo("some-topic")
                .WithMessageExpiration(TimeSpan.FromMinutes(42));
            var endpoint = builder.Build();

            endpoint.MessageExpiryInterval.Should().Be(42 * 60);
        }
    }
}
