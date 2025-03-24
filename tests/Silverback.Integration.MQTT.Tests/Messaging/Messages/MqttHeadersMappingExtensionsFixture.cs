// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using MQTTnet.Packets;
using Shouldly;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Messages;

public class MqttHeadersMappingExtensionsFixture
{
    [Fact]
    public void ToUserProperties_ShouldMapHeaders()
    {
        MessageHeaderCollection headers = new()
        {
            { "one", "1" },
            { "two", "2" }
        };

        List<MqttUserProperty> userProperties = headers.ToUserProperties();

        userProperties.ShouldBe(
        [
            new MqttUserProperty("one", "1"),
            new MqttUserProperty("two", "2")
        ]);
    }

    [Fact]
    public void ToUserProperties_ShouldMapMessageId()
    {
        MessageHeaderCollection headers = new()
        {
            { "one", "1" },
            { DefaultMessageHeaders.MessageId, "1234" },
            { "two", "2" }
        };

        List<MqttUserProperty> userProperties = headers.ToUserProperties();

        userProperties.ShouldBe(
        [
            new MqttUserProperty("one", "1"),
            new MqttUserProperty(DefaultMessageHeaders.MessageId, "1234"),
            new MqttUserProperty("two", "2")
        ]);
    }

    [Fact]
    public void ToUserProperties_ShouldIgnoreMqttResponseHeaders()
    {
        MessageHeaderCollection headers = new()
        {
            { "one", "1" },
            { MqttMessageHeaders.ResponseTopic, "aaa" },
            { MqttMessageHeaders.CorrelationData, "bbb" },
            { "two", "2" }
        };

        List<MqttUserProperty> userProperties = headers.ToUserProperties();

        userProperties.ShouldBe(
        [
            new MqttUserProperty("one", "1"),
            new MqttUserProperty("two", "2")
        ]);
    }

    [Fact]
    public void ToUserProperties_ShouldIgnoreDestinationTopicHeader()
    {
        MessageHeaderCollection headers = new()
        {
            { "one", "1" },
            { MqttMessageHeaders.DestinationTopic, "topic1" },
            { "two", "2" }
        };

        List<MqttUserProperty> userProperties = headers.ToUserProperties();

        userProperties.ShouldBe(
        [
            new MqttUserProperty("one", "1"),
            new MqttUserProperty("two", "2")
        ]);
    }

    [Fact]
    public void ToUserProperties_ShouldIgnoreInternalHeaders()
    {
        MessageHeaderCollection headers = new()
        {
            { "one", "1" },
            { DefaultMessageHeaders.InternalHeadersPrefix + "aaa", "aaa" },
            { DefaultMessageHeaders.InternalHeadersPrefix + "bbb", "bbb" },
            { "two", "2" }
        };

        List<MqttUserProperty> userProperties = headers.ToUserProperties();

        userProperties.ShouldBe(
        [
            new MqttUserProperty("one", "1"),
            new MqttUserProperty("two", "2")
        ]);
    }
}
