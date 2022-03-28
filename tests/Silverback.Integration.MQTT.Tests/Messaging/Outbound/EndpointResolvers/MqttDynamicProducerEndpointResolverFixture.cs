// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Outbound.EndpointResolvers;

public class MqttDynamicProducerEndpointResolverFixture
{
    [Fact]
    public void GetEndpoint_ShouldReturnTopicFromTopicNameFunction()
    {
        MqttDynamicProducerEndpointResolver endpointResolver = new(_ => "topic");

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(null, new MqttProducerEndpointConfiguration(), Substitute.For<IServiceProvider>());

        endpoint.Should().NotBeNull();
        endpoint.Should().BeOfType<MqttProducerEndpoint>();
        endpoint.As<MqttProducerEndpoint>().Topic.Should().Be("topic");
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicFromTopicNameFormat()
    {
        MqttDynamicProducerEndpointResolver endpointResolver = new("topic-{0}", _ => new[] { "123" });

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(null, new MqttProducerEndpointConfiguration(), Substitute.For<IServiceProvider>());

        endpoint.Should().NotBeNull();
        endpoint.Should().BeOfType<MqttProducerEndpoint>();
        endpoint.As<MqttProducerEndpoint>().Topic.Should().Be("topic-123");
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicFromResolver()
    {
        MqttDynamicProducerEndpointResolver endpointResolver = new(
            typeof(TestEndpointResolver),
            (message, serviceProvider) => serviceProvider.GetRequiredService<TestEndpointResolver>()
                .GetTopic((TestEventOne?)message));

        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(TestEndpointResolver)).Returns(new TestEndpointResolver());

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(null, new MqttProducerEndpointConfiguration(), serviceProvider);

        endpoint.Should().NotBeNull();
        endpoint.Should().BeOfType<MqttProducerEndpoint>();
        endpoint.As<MqttProducerEndpoint>().Topic.Should().Be("topic");
    }

    [Fact]
    public void RawName_ShouldReturnPlaceholderFromTopicNameFunction()
    {
        MqttDynamicProducerEndpointResolver endpointResolver = new(_ => "topic");

        endpointResolver.RawName.Should().StartWith("dynamic-");
    }

    [Fact]
    public void RawName_ShouldReturnFormatStringFromTopicFormat()
    {
        MqttDynamicProducerEndpointResolver endpointResolver = new("topic-{0}", _ => new[] { "123" });

        endpointResolver.RawName.Should().StartWith("topic-{0}");
    }

    [Fact]
    public void RawName_ShouldReturnPlaceholderWithTypeNameFromResolver()
    {
        MqttDynamicProducerEndpointResolver endpointResolver = new(
            typeof(TestEndpointResolver),
            (message, serviceProvider) => serviceProvider.GetRequiredService<TestEndpointResolver>().GetTopic((TestEventOne?)message));

        endpointResolver.RawName.Should().StartWith("dynamic-TestEndpointResolver-");
    }

    [Fact]
    public async Task SerializeAsync_ShouldSerializeTargetTopic()
    {
        MqttDynamicProducerEndpointResolver endpointResolver = new(_ => "abc");
        MqttProducerEndpoint endpoint = new("topic", new MqttProducerEndpointConfiguration());

        byte[] result = await endpointResolver.SerializeAsync(endpoint);

        Encoding.UTF8.GetString(result).Should().Be("topic");
    }

    [Fact]
    public async Task DeserializeAsync_ShouldDeserializeEndpoint()
    {
        MqttDynamicProducerEndpointResolver endpointResolver = new(_ => "abc");
        byte[] serialized = Encoding.UTF8.GetBytes("topic");

        MqttProducerEndpoint result = await endpointResolver.DeserializeAsync(serialized, new MqttProducerEndpointConfiguration());
        result.Should().NotBeNull();
        result.Topic.Should().Be("topic");
    }

    private sealed class TestEndpointResolver : IMqttProducerEndpointResolver<TestEventOne>
    {
        public string GetTopic(TestEventOne? message) => "topic";
    }
}
