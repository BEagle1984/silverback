﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Collections;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttConsumerEndpointsCacheFixture
{
    [Fact]
    public void GetEndpoint_ShouldReturnEndpoint()
    {
        MqttConsumerEndpointConfiguration endpointConfiguration1 = new()
        {
            Topics = new ValueReadOnlyCollection<string>(["topic1/+", "topic2"])
        };
        MqttConsumerEndpointConfiguration endpointConfiguration2 = new()
        {
            Topics = new ValueReadOnlyCollection<string>(["topic3/#", "topic4"])
        };
        MqttConsumerEndpointsCache cache = new(
            new MqttClientConfiguration
            {
                ConsumerEndpoints = new ValueReadOnlyCollection<MqttConsumerEndpointConfiguration>(
                [
                    endpointConfiguration1,
                    endpointConfiguration2
                ])
            });

        MqttConsumerEndpoint endpoint = cache.GetEndpoint("topic4");

        endpoint.Should().NotBeNull();
        endpoint.Topic.Should().Be("topic4");
        endpoint.Configuration.Should().BeSameAs(endpointConfiguration2);
    }

    [Theory]
    [InlineData("topic1/sub")]
    [InlineData("topic2/sub/sub")]
    [InlineData("topic3/sub/sub/sub/sub")]
    public void GetEndpoint_ShouldReturnEndpointFromWildcardSubscription(string topic)
    {
        MqttConsumerEndpointConfiguration endpointConfiguration1 = new()
        {
            Topics = new ValueReadOnlyCollection<string>(["topicA", "topicB"])
        };
        MqttConsumerEndpointConfiguration endpointConfiguration2 = new()
        {
            Topics = new ValueReadOnlyCollection<string>(["topic1/+", "topic2/+/sub", "topic3/#"])
        };
        MqttConsumerEndpointsCache cache = new(
            new MqttClientConfiguration
            {
                ConsumerEndpoints = new ValueReadOnlyCollection<MqttConsumerEndpointConfiguration>(
                [
                    endpointConfiguration1,
                    endpointConfiguration2
                ])
            });

        MqttConsumerEndpoint endpoint = cache.GetEndpoint(topic);

        endpoint.Should().NotBeNull();
        endpoint.Topic.Should().Be(topic);
        endpoint.Configuration.Should().BeSameAs(endpointConfiguration2);
    }

    [Theory]
    [InlineData("topicB")]
    [InlineData("topic2/test/sub")]
    public void GetEndpoint_ShouldReturnCachedEndpointInstance(string topic)
    {
        MqttConsumerEndpointConfiguration endpointConfiguration1 = new()
        {
            Topics = new ValueReadOnlyCollection<string>(["topicA", "topicB"])
        };
        MqttConsumerEndpointConfiguration endpointConfiguration2 = new()
        {
            Topics = new ValueReadOnlyCollection<string>(["topic1/+", "topic2/+/sub", "topic3/#"])
        };
        MqttConsumerEndpointsCache cache = new(
            new MqttClientConfiguration
            {
                ConsumerEndpoints = new ValueReadOnlyCollection<MqttConsumerEndpointConfiguration>(
                [
                    endpointConfiguration1,
                    endpointConfiguration2
                ])
            });

        MqttConsumerEndpoint endpoint1 = cache.GetEndpoint(topic);
        MqttConsumerEndpoint endpoint2 = cache.GetEndpoint(topic);

        endpoint2.Should().BeSameAs(endpoint1);
    }

    [Fact]
    public void GetEndpoint_ShouldThrow_WhenNoConfigurationFound()
    {
        MqttConsumerEndpointConfiguration endpointConfiguration1 = new()
        {
            Topics = new ValueReadOnlyCollection<string>(["topic1/+", "topic2"])
        };
        MqttConsumerEndpointConfiguration endpointConfiguration2 = new()
        {
            Topics = new ValueReadOnlyCollection<string>(["topic3/#", "topic4"])
        };
        MqttConsumerEndpointsCache cache = new(
            new MqttClientConfiguration
            {
                ConsumerEndpoints = new ValueReadOnlyCollection<MqttConsumerEndpointConfiguration>(
                [
                    endpointConfiguration1,
                    endpointConfiguration2
                ])
            });

        Action act = () => cache.GetEndpoint("topic5");

        act.Should().Throw<InvalidOperationException>().WithMessage("No configuration found for the specified topic 'topic5'.");
    }
}
