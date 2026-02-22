// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using MQTTnet.Packets;
using Shouldly;
using Silverback.Messaging.Messages;
using Xunit;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Silverback.Tests.Integration.Mqtt.Messaging.Messages;

public class MqttHeadersMappingExtensionsTests
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
