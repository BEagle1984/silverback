// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using MQTTnet.Protocol;
using Newtonsoft.Json;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt
{
    public class MqttLastWillMessageBuilderTests
    {
        [Fact]
        public void Build_WithoutTopicName_ExceptionThrown()
        {
            var builder = new MqttLastWillMessageBuilder();

            Action act = () => builder.Build();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void ProduceTo_TopicName_TopicSet()
        {
            var builder = new MqttLastWillMessageBuilder();

            builder.ProduceTo("testaments");

            var willMessage = builder.Build();
            willMessage.Topic.Should().Be("testaments");
        }

        [Fact]
        public async Task Message_SomeMessage_PayloadSet()
        {
            var message = new TestEventOne { Content = "Hello MQTT!" };
            var builder = new MqttLastWillMessageBuilder();

            builder
                .ProduceTo("testaments")
                .Message(message);

            var willMessage = builder.Build();
            willMessage.Payload.Should().NotBeNullOrEmpty();

            (object? deserializedMessage, var _) = await new JsonMessageSerializer<TestEventOne>().DeserializeAsync(
                new MemoryStream(willMessage.Payload),
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            deserializedMessage.Should().BeEquivalentTo(message);
        }

        [Fact]
        public void WithDelay_TimeSpam_DelaySet()
        {
            var builder = new MqttLastWillMessageBuilder();

            builder
                .ProduceTo("testaments")
                .WithDelay(TimeSpan.FromSeconds(42));

            builder.Delay.Should().Be(42);
        }

        [Fact]
        public void WithQualityOfServiceLevel_QosLevelSet()
        {
            var builder = new MqttLastWillMessageBuilder();

            builder
                .ProduceTo("testaments")
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce);

            var willMessage = builder.Build();
            willMessage.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.ExactlyOnce);
        }

        [Fact]
        public void WithAtMostOnceQoS_QosLevelSet()
        {
            var builder = new MqttLastWillMessageBuilder();

            builder
                .ProduceTo("testaments")
                .WithAtMostOnceQoS();

            var willMessage = builder.Build();
            willMessage.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtMostOnce);
        }

        [Fact]
        public void WithAtLeastOnceQoS_QosLevelSet()
        {
            var builder = new MqttLastWillMessageBuilder();

            builder
                .ProduceTo("testaments")
                .WithAtLeastOnceQoS();

            var willMessage = builder.Build();
            willMessage.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtLeastOnce);
        }

        [Fact]
        public void WithExactlyOnceQoS_QosLevelSet()
        {
            var builder = new MqttLastWillMessageBuilder();

            builder
                .ProduceTo("testaments")
                .WithExactlyOnceQoS();

            var willMessage = builder.Build();
            willMessage.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.ExactlyOnce);
        }

        [Fact]
        public void WithRetain_RetainFlagSet()
        {
            var builder = new MqttLastWillMessageBuilder();

            builder
                .ProduceTo("testaments")
                .Retain();

            var willMessage = builder.Build();
            willMessage.Retain.Should().BeTrue();
        }

        [Fact]
        public async Task SerializeUsing_NewtonsoftJsonSerializer_PayloadSerialized()
        {
            var message = new TestEventOne { Content = "Hello MQTT!" };
            var serializer = new NewtonsoftJsonMessageSerializer<TestEventOne>
            {
                Encoding = MessageEncoding.Unicode,
                Settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented
                }
            };
            var messageBytes = (await serializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty)).ReadAll();
            var builder = new MqttLastWillMessageBuilder();

            builder
                .ProduceTo("testaments")
                .SerializeUsing(serializer)
                .Message(message);

            var willMessage = builder.Build();
            willMessage.Payload.Should().NotBeNullOrEmpty();
            willMessage.Payload.Should().BeEquivalentTo(messageBytes);
        }

        [Fact]
        public async Task SerializeAsJson_Action_PayloadSerialized()
        {
            var message = new TestEventOne { Content = "Hello MQTT!" };
            var serializer = new JsonMessageSerializer<TestEventOne>
            {
                Options = new JsonSerializerOptions
                {
                    WriteIndented = true
                }
            };
            var messageBytes = (await serializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty)).ReadAll();
            var builder = new MqttLastWillMessageBuilder();

            builder
                .ProduceTo("testaments")
                .SerializeAsJson(
                    serializerBuilder => serializerBuilder
                        .WithOptions(
                            new JsonSerializerOptions
                            {
                                WriteIndented = true
                            }))
                .Message(message);

            var willMessage = builder.Build();
            willMessage.Payload.Should().NotBeNullOrEmpty();
            willMessage.Payload.Should().BeEquivalentTo(messageBytes);
        }
    }
}
