// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;

namespace Silverback.Tests.Types;

public class TestConsumerConfigurationBuilder<TMessage>
    : ConsumerConfigurationBuilder<TMessage, TestConsumerConfiguration, TestConsumerConfigurationBuilder<TMessage>>
{
    public TestConsumerConfigurationBuilder(EndpointsConfigurationBuilder? endpointsConfigurationBuilder = null)
        : base(endpointsConfigurationBuilder)
    {
    }

    public override string EndpointRawName => "test";

    protected override TestConsumerConfigurationBuilder<TMessage> This => this;

    protected override TestConsumerConfiguration CreateConfiguration() => new("test");
}
