// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
using Silverback.Collections;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttConsumerEndpointsCacheTests
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

        endpoint.ShouldNotBeNull();
        endpoint.Topic.ShouldBe("topic4");
        endpoint.Configuration.ShouldBeSameAs(endpointConfiguration2);
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

        endpoint.ShouldNotBeNull();
        endpoint.Topic.ShouldBe(topic);
        endpoint.Configuration.ShouldBeSameAs(endpointConfiguration2);
    }

    [Fact]
    public void GetEndpoint_ShouldReturnCachedEndpointInstance()
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

        MqttConsumerEndpoint endpoint1 = cache.GetEndpoint("topicB");
        MqttConsumerEndpoint endpoint2 = cache.GetEndpoint("topicB");

        endpoint2.ShouldBeSameAs(endpoint1);
    }

    [Fact]
    public void GetEndpoint_ShouldNotCacheWildcardEndpointInstance()
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

        MqttConsumerEndpoint endpoint1 = cache.GetEndpoint("topic2/test/sub");
        MqttConsumerEndpoint endpoint2 = cache.GetEndpoint("topic2/test/sub");

        endpoint2.ShouldNotBeSameAs(endpoint1);
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

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("No configuration found for the specified topic 'topic5'.");
    }

    [Fact]
    public void GetEndpoint_ShouldReturnEndpointFromWildcardSubscription_WhenTopicIsParentLevel()
    {
        MqttConsumerEndpointConfiguration endpointConfiguration = new()
        {
            Topics = new ValueReadOnlyCollection<string>(["topic1/#"])
        };
        MqttConsumerEndpointsCache cache = new(
            new MqttClientConfiguration
            {
                ConsumerEndpoints = new ValueReadOnlyCollection<MqttConsumerEndpointConfiguration>([endpointConfiguration])
            });

        MqttConsumerEndpoint endpoint = cache.GetEndpoint("topic1");

        endpoint.Topic.ShouldBe("topic1");
        endpoint.Configuration.ShouldBeSameAs(endpointConfiguration);
    }

    [Fact]
    public void GetEndpoint_ShouldPreferTopicNameOverWildcardSubscription()
    {
        MqttConsumerEndpointConfiguration endpointConfiguration1 = new()
        {
            Topics = new ValueReadOnlyCollection<string>(["topic/#"])
        };
        MqttConsumerEndpointConfiguration endpointConfiguration2 = new()
        {
            Topics = new ValueReadOnlyCollection<string>(["topic/sub"])
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

        MqttConsumerEndpoint wildcardEndpoint = cache.GetEndpoint("topic/other");
        MqttConsumerEndpoint topicEndpoint = cache.GetEndpoint("topic/sub");

        wildcardEndpoint.Configuration.ShouldBeSameAs(endpointConfiguration1);
        topicEndpoint.Configuration.ShouldBeSameAs(endpointConfiguration2);
    }

    [Fact]
    public void GetEndpoint_ShouldReturnEndpointFromFirstMatchingWildcardSubscription()
    {
        MqttConsumerEndpointConfiguration endpointConfiguration1 = new()
        {
            Topics = new ValueReadOnlyCollection<string>(["topic/+/sub"])
        };
        MqttConsumerEndpointConfiguration endpointConfiguration2 = new()
        {
            Topics = new ValueReadOnlyCollection<string>(["topic/#"])
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

        MqttConsumerEndpoint firstEndpoint = cache.GetEndpoint("topic/1/sub");
        MqttConsumerEndpoint secondEndpoint = cache.GetEndpoint("topic/1/other");

        firstEndpoint.Configuration.ShouldBeSameAs(endpointConfiguration1);
        secondEndpoint.Configuration.ShouldBeSameAs(endpointConfiguration2);
    }

    [Fact]
    public void GetEndpoint_ShouldNotReturnLeadingWildcardEndpoint_WhenTopicStartsWithDollar()
    {
        MqttConsumerEndpointConfiguration endpointConfiguration1 = new()
        {
            Topics = new ValueReadOnlyCollection<string>(["#"])
        };
        MqttConsumerEndpointConfiguration endpointConfiguration2 = new()
        {
            Topics = new ValueReadOnlyCollection<string>(["$SYS/#"])
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

        MqttConsumerEndpoint endpoint = cache.GetEndpoint("$SYS/uptime");

        endpoint.Configuration.ShouldBeSameAs(endpointConfiguration2);
    }

    [Fact]
    public void GetEndpoint_ShouldReturnEndpointFromSharedSubscription()
    {
        MqttConsumerEndpointConfiguration endpointConfiguration1 = new()
        {
            Topics = new ValueReadOnlyCollection<string>(["$share/group/topic1"])
        };
        MqttConsumerEndpointConfiguration endpointConfiguration2 = new()
        {
            Topics = new ValueReadOnlyCollection<string>(["$share/group/topic2/+"])
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

        MqttConsumerEndpoint endpoint1 = cache.GetEndpoint("topic1");
        MqttConsumerEndpoint endpoint2 = cache.GetEndpoint("topic2/sub");

        endpoint1.Topic.ShouldBe("topic1");
        endpoint1.Configuration.ShouldBeSameAs(endpointConfiguration1);
        endpoint2.Topic.ShouldBe("topic2/sub");
        endpoint2.Configuration.ShouldBeSameAs(endpointConfiguration2);
    }
}
