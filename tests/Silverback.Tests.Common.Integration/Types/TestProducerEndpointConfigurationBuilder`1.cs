// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;

namespace Silverback.Tests.Types;

public class TestProducerEndpointConfigurationBuilder<TMessage>
    : ProducerEndpointConfigurationBuilder<TMessage, TestProducerEndpointConfiguration, TestProducerEndpoint, TestProducerEndpointConfigurationBuilder<TMessage>>
{
    private string _topic = "test";

    public TestProducerEndpointConfigurationBuilder(IServiceProvider serviceProvider, string? friendlyName = null)
        : base(serviceProvider, friendlyName)
    {
    }

    protected override TestProducerEndpointConfigurationBuilder<TMessage> This => this;

    public TestProducerEndpointConfigurationBuilder<TMessage> ProduceTo(string topic)
    {
        _topic = topic;
        return this;
    }

    protected override TestProducerEndpointConfiguration CreateConfiguration() => new()
    {
        EndpointResolver = new TestStaticProducerEndpointResolver(_topic)
    };
}
