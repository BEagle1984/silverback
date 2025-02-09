// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MQTTnet.Protocol;
using Newtonsoft.Json;
using NSubstitute;
using Shouldly;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttLastWillMessageConfigurationBuilderFixture
{
    [Fact]
    public void ProduceTo_ShoudSetTopic()
    {
        MqttLastWillMessageConfigurationBuilder<TestEventOne> configurationBuilder = new();

        configurationBuilder.ProduceTo("testaments");

        MqttLastWillMessageConfiguration willMessage = configurationBuilder.Build();
        willMessage.Topic.ShouldBe("testaments");
    }

    [Fact]
    public async Task SendMessage_ShouldSetPayload()
    {
        TestEventOne message = new() { Content = "Hello MQTT!" };
        MqttLastWillMessageConfigurationBuilder<TestEventOne> configurationBuilder = new();

        configurationBuilder
            .ProduceTo("testaments")
            .SendMessage(message);

        MqttLastWillMessageConfiguration willMessage = configurationBuilder.Build();
        willMessage.Payload.ShouldNotBeEmpty();

        (object? deserializedMessage, Type _) = await new JsonMessageDeserializer<TestEventOne>().DeserializeAsync(
            new MemoryStream(willMessage.Payload!),
            [],
            TestConsumerEndpoint.GetDefault());

        deserializedMessage.ShouldBeEquivalentTo(message);
    }

    [Fact]
    public void WithDelay_ShouldSetDelay()
    {
        MqttLastWillMessageConfigurationBuilder<TestEventOne> configurationBuilder = new();

        configurationBuilder
            .ProduceTo("testaments")
            .WithDelay(TimeSpan.FromSeconds(42));

        MqttLastWillMessageConfiguration willMessage = configurationBuilder.Build();
        willMessage.Delay.ShouldBe(42U);
    }

    [Fact]
    public void WithExpiration_ShouldSetExpiration()
    {
        MqttLastWillMessageConfigurationBuilder<TestEventOne> configurationBuilder = new();

        configurationBuilder
            .ProduceTo("testaments")
            .WithExpiration(TimeSpan.FromSeconds(42));

        MqttLastWillMessageConfiguration willMessage = configurationBuilder.Build();
        willMessage.Expiration.ShouldBe(42U);
    }

    [Fact]
    public void WithQualityOfServiceLevel_ShouldSetQosLevel()
    {
        MqttLastWillMessageConfigurationBuilder<TestEventOne> configurationBuilder = new();

        configurationBuilder
            .ProduceTo("testaments")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce);

        MqttLastWillMessageConfiguration willMessage = configurationBuilder.Build();
        willMessage.QualityOfServiceLevel.ShouldBe(MqttQualityOfServiceLevel.ExactlyOnce);
    }

    [Fact]
    public void WithAtMostOnceQoS_ShouldSetQosLevel()
    {
        MqttLastWillMessageConfigurationBuilder<TestEventOne> configurationBuilder = new();

        configurationBuilder
            .ProduceTo("testaments")
            .WithAtMostOnceQoS();

        MqttLastWillMessageConfiguration willMessage = configurationBuilder.Build();
        willMessage.QualityOfServiceLevel.ShouldBe(MqttQualityOfServiceLevel.AtMostOnce);
    }

    [Fact]
    public void WithAtLeastOnceQoS_ShouldSetQosLevel()
    {
        MqttLastWillMessageConfigurationBuilder<TestEventOne> configurationBuilder = new();

        configurationBuilder
            .ProduceTo("testaments")
            .WithAtLeastOnceQoS();

        MqttLastWillMessageConfiguration willMessage = configurationBuilder.Build();
        willMessage.QualityOfServiceLevel.ShouldBe(MqttQualityOfServiceLevel.AtLeastOnce);
    }

    [Fact]
    public void WithExactlyOnceQoS_ShouldSetQosLevel()
    {
        MqttLastWillMessageConfigurationBuilder<TestEventOne> configurationBuilder = new();

        configurationBuilder
            .ProduceTo("testaments")
            .WithExactlyOnceQoS();

        MqttLastWillMessageConfiguration willMessage = configurationBuilder.Build();
        willMessage.QualityOfServiceLevel.ShouldBe(MqttQualityOfServiceLevel.ExactlyOnce);
    }

    [Fact]
    public void WithRetain_ShouldSetRetainFlag()
    {
        MqttLastWillMessageConfigurationBuilder<TestEventOne> configurationBuilder = new();

        configurationBuilder
            .ProduceTo("testaments")
            .Retain();

        MqttLastWillMessageConfiguration willMessage = configurationBuilder.Build();
        willMessage.Retain.ShouldBeTrue();
    }

    [Fact]
    public async Task SerializeUsing_ShouldSetSerializer()
    {
        TestEventOne message = new() { Content = "Hello MQTT!" };
        IMessageSerializer serializer = Substitute.For<IMessageSerializer>();
        byte[] payload = "test"u8.ToArray();
        serializer.SerializeAsync(message, Arg.Any<MessageHeaderCollection>(), Arg.Any<ProducerEndpoint>())
            .Returns(new MemoryStream(payload));

        MqttLastWillMessageConfigurationBuilder<TestEventOne> configurationBuilder = new();

        configurationBuilder
            .ProduceTo("testaments")
            .SerializeUsing(serializer)
            .SendMessage(message);

        MqttLastWillMessageConfiguration willMessage = configurationBuilder.Build();
        await serializer.Received(1).SerializeAsync(
            message,
            Arg.Any<MessageHeaderCollection>(),
            Arg.Any<ProducerEndpoint>());
        willMessage.Payload.ShouldBe(payload);
    }

    [Fact]
    public async Task SerializeAsJson_ShouldSetSerializer()
    {
        TestEventOne message = new() { Content = "Hello MQTT!" };
        JsonMessageSerializer serializer = new(
            new JsonSerializerOptions
            {
                WriteIndented = true
            });
        byte[]? messageBytes = (await serializer.SerializeAsync(
            message,
            [],
            TestProducerEndpoint.GetDefault())).ReadAll();
        MqttLastWillMessageConfigurationBuilder<TestEventOne> configurationBuilder = new();

        configurationBuilder
            .ProduceTo("testaments")
            .SerializeAsJson(
                serializerBuilder => serializerBuilder
                    .Configure(
                        options =>
                        {
                            options.WriteIndented = true;
                        }))
            .SendMessage(message);

        MqttLastWillMessageConfiguration willMessage = configurationBuilder.Build();
        willMessage.Payload.ShouldNotBeEmpty();
        willMessage.Payload.ShouldBe(messageBytes);
    }

    [Fact]
    public async Task SerializeAsJsonUsingNewtonsoft_ShouldSetSerializer()
    {
        TestEventOne message = new() { Content = "Hello MQTT!" };
        NewtonsoftJsonMessageSerializer serializer = new(
            new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            });
        byte[]? messageBytes = (await serializer.SerializeAsync(
            message,
            [],
            TestProducerEndpoint.GetDefault())).ReadAll();
        MqttLastWillMessageConfigurationBuilder<TestEventOne> configurationBuilder = new();

        configurationBuilder
            .ProduceTo("testaments")
            .SerializeAsJsonUsingNewtonsoft(
                serializerBuilder => serializerBuilder
                    .Configure(
                        settings =>
                        {
                            settings.Formatting = Formatting.Indented;
                        }))
            .SendMessage(message);

        MqttLastWillMessageConfiguration willMessage = configurationBuilder.Build();
        willMessage.Payload.ShouldNotBeEmpty();
        willMessage.Payload.ShouldBe(messageBytes);
    }

    [Fact]
    public void WithContentType_ShouldSetContentType()
    {
        MqttLastWillMessageConfigurationBuilder<TestEventOne> configurationBuilder = new();

        configurationBuilder
            .ProduceTo("testaments")
            .WithContentType("application/json");

        MqttLastWillMessageConfiguration willMessage = configurationBuilder.Build();
        willMessage.ContentType.ShouldBe("application/json");
    }

    [Fact]
    public void WithCorrelationData_ShouldSetCorrelationData()
    {
        MqttLastWillMessageConfigurationBuilder<TestEventOne> configurationBuilder = new();
        byte[] correlationData = [1, 2, 3];

        configurationBuilder
            .ProduceTo("testaments")
            .WithCorrelationData(correlationData);

        MqttLastWillMessageConfiguration willMessage = configurationBuilder.Build();
        willMessage.CorrelationData.ShouldBeSameAs(correlationData);
    }

    [Fact]
    public void WithPayloadFormatIndicator_ShouldSetPayloadFormatIndicator()
    {
        MqttLastWillMessageConfigurationBuilder<TestEventOne> configurationBuilder = new();

        configurationBuilder
            .ProduceTo("testaments")
            .WithPayloadFormatIndicator(MqttPayloadFormatIndicator.CharacterData);

        MqttLastWillMessageConfiguration willMessage = configurationBuilder.Build();
        willMessage.PayloadFormatIndicator.ShouldBe(MqttPayloadFormatIndicator.CharacterData);
    }

    [Fact]
    public void AddHeader_ShouldAddUserProperty()
    {
        MqttLastWillMessageConfigurationBuilder<TestEventOne> configurationBuilder = new();

        configurationBuilder
            .ProduceTo("testaments")
            .AddHeader("one", "1")
            .AddHeader("two", "2");

        MqttLastWillMessageConfiguration willMessage = configurationBuilder.Build();
        willMessage.UserProperties.Count.ShouldBe(2);
        willMessage.UserProperties.ShouldContain(new MqttUserProperty("one", "1"));
        willMessage.UserProperties.ShouldContain(new MqttUserProperty("two", "2"));
    }
}
