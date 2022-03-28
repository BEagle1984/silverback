// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;

namespace Silverback.Tests.Types;

public class TestConsumerEndpointConfigurationBuilder<TMessage>
    : ConsumerEndpointConfigurationBuilder<TMessage, TestConsumerEndpointConfiguration, TestConsumerEndpointConfigurationBuilder<TMessage>>
{
    public TestConsumerEndpointConfigurationBuilder(string? friendlyName = null)
        : base(friendlyName)
    {
    }

    public override string EndpointRawName => "test";

    protected override TestConsumerEndpointConfigurationBuilder<TMessage> This => this;

    protected override TestConsumerEndpointConfiguration CreateConfiguration() => new("test");
}
