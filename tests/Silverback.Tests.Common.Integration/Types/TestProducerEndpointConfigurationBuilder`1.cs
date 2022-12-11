// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;

namespace Silverback.Tests.Types;

public class TestProducerEndpointConfigurationBuilder<TMessage>
    : ProducerEndpointConfigurationBuilder<TMessage, TestProducerEndpointConfiguration, TestProducerEndpoint, TestProducerEndpointConfigurationBuilder<TMessage>>
{
    private string _topic = "test";

    public TestProducerEndpointConfigurationBuilder(string? friendlyName = null)
        : base(friendlyName)
    {
    }

    public override string EndpointRawName => _topic;

    protected override TestProducerEndpointConfigurationBuilder<TMessage> This => this;

    public TestProducerEndpointConfigurationBuilder<TMessage> ProduceTo(string topic)
    {
        _topic = topic;
        return this;
    }

    protected override TestProducerEndpointConfiguration CreateConfiguration() => new()
    {
        Endpoint = new TestStaticProducerEndpointResolver(_topic)
    };
}
