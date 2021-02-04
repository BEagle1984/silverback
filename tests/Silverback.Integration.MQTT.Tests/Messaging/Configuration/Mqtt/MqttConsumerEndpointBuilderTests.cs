// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
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

        private readonly MqttEventsHandlers _mqttEventsHandlers = new()
        {
            ConnectedHandler = _ => Task.CompletedTask,
            DisconnectingHandler = _ => Task.CompletedTask
        };

        [Fact]
        public void Build_WithoutTopicName_ExceptionThrown()
        {
            var builder = new MqttConsumerEndpointBuilder(_clientConfig, _mqttEventsHandlers);

            Action act = () => builder.Build();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void Build_WithoutServer_ExceptionThrown()
        {
            var builder = new MqttConsumerEndpointBuilder(new MqttClientConfig(), _mqttEventsHandlers);

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
            var builder = new MqttConsumerEndpointBuilder(_clientConfig, _mqttEventsHandlers);
            builder.ConsumeFrom("some-topic");
            var endpoint = builder.Build();

            endpoint.Name.Should().Be("some-topic");
        }

        [Fact]
        public void ConsumeFrom_MultipleTopics_TopicsSet()
        {
            var builder = new MqttConsumerEndpointBuilder(_clientConfig, _mqttEventsHandlers);
            builder.ConsumeFrom("some-topic", "some-other-topic");
            var endpoint = builder.Build();

            endpoint.Topics.Should().BeEquivalentTo("some-topic", "some-other-topic");
        }

        [Fact]
        public void Configure_ConfigAction_ConfigurationMergedWithBase()
        {
            var builder = new MqttConsumerEndpointBuilder(_clientConfig, _mqttEventsHandlers);
            builder
                .ConsumeFrom("some-topic")
                .Configure(config => config.ClientId = "client42");
            var endpoint = builder.Build();

            endpoint.Configuration.ChannelOptions.Should().NotBeNull();
            endpoint.Configuration.ChannelOptions.Should().BeSameAs(_clientConfig.ChannelOptions);
            endpoint.Configuration.ClientId.Should().Be("client42");
        }

        [Fact]
        public void Configure_BuilderAction_ConfigurationMergedWithBase()
        {
            var builder = new MqttConsumerEndpointBuilder(_clientConfig, _mqttEventsHandlers);
            builder
                .ConsumeFrom("some-topic")
                .Configure(config => config.WithClientId("client42"));
            var endpoint = builder.Build();

            endpoint.Configuration.ChannelOptions.Should().NotBeNull();
            endpoint.Configuration.ChannelOptions.Should().BeEquivalentTo(_clientConfig.ChannelOptions);
            endpoint.Configuration.ClientId.Should().Be("client42");
        }

        [Fact]
        public void BindEvents_EventsHandlerAction_HandlersMergedWithBase()
        {
            Func<MqttClientConfig, Task> callback = _ => Task.CompletedTask;

            var builder = new MqttConsumerEndpointBuilder(_clientConfig, _mqttEventsHandlers);
            builder
                .ConsumeFrom("some-topic")
                .BindEvents(handlers => handlers.ConnectedHandler = callback);
            var endpoint = builder.Build();

            endpoint.EventsHandlers.ConnectedHandler.Should().BeSameAs(callback);
            endpoint.EventsHandlers.DisconnectingHandler.Should()
                .BeSameAs(_mqttEventsHandlers.DisconnectingHandler); // Reference comparison
        }

        [Fact]
        public void BindEvents_EventsHandlerBuilderAction_HandlersMergedWithBase()
        {
            Func<MqttClientConfig, Task> callback = _ => Task.CompletedTask;

            var builder = new MqttConsumerEndpointBuilder(_clientConfig, _mqttEventsHandlers);
            builder
                .ConsumeFrom("some-topic")
                .BindEvents(b => b.OnConnected(callback));
            var endpoint = builder.Build();

            endpoint.EventsHandlers.ConnectedHandler.Should().BeSameAs(callback);
            endpoint.EventsHandlers.DisconnectingHandler.Should()
                .Be(_mqttEventsHandlers.DisconnectingHandler); // Value comparison
        }

        [Fact]
        public void WithQualityOfServiceLevel_QualityOfServiceLevelSet()
        {
            var builder = new MqttConsumerEndpointBuilder(_clientConfig, _mqttEventsHandlers);
            builder
                .ConsumeFrom("some-topic")
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);
            var endpoint = builder.Build();

            endpoint.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtLeastOnce);
        }

        [Fact]
        public void WithAtMostOnceQoS_QualityOfServiceLevelSet()
        {
            var builder = new MqttConsumerEndpointBuilder(_clientConfig, _mqttEventsHandlers);
            builder
                .ConsumeFrom("some-topic")
                .WithAtMostOnceQoS();
            var endpoint = builder.Build();

            endpoint.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtMostOnce);
        }

        [Fact]
        public void WithAtLeastOnceQoS_QualityOfServiceLevelSet()
        {
            var builder = new MqttConsumerEndpointBuilder(_clientConfig, _mqttEventsHandlers);
            builder
                .ConsumeFrom("some-topic")
                .WithAtLeastOnceQoS();
            var endpoint = builder.Build();

            endpoint.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtLeastOnce);
        }

        [Fact]
        public void WithExactlyOnceQoS_QualityOfServiceLevelSet()
        {
            var builder = new MqttConsumerEndpointBuilder(_clientConfig, _mqttEventsHandlers);
            builder
                .ConsumeFrom("some-topic")
                .WithExactlyOnceQoS();
            var endpoint = builder.Build();

            endpoint.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.ExactlyOnce);
        }

        [Fact]
        public void WithEventsHandlers_EventsHandlersSet()
        {
            var builder = new MqttConsumerEndpointBuilder(_clientConfig, _mqttEventsHandlers);
            builder
                .ConsumeFrom("some-topic");
            var endpoint = builder.Build();

            endpoint.EventsHandlers.Should().BeSameAs(_mqttEventsHandlers);
        }
    }
}
