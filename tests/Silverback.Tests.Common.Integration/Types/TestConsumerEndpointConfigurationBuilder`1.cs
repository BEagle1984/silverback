// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;

namespace Silverback.Tests.Types;

public class TestConsumerEndpointConfigurationBuilder<TMessage>
    : ConsumerEndpointConfigurationBuilder<TMessage, TestConsumerEndpointConfiguration, TestConsumerEndpointConfigurationBuilder<TMessage>>
{
    public TestConsumerEndpointConfigurationBuilder(IServiceProvider serviceProvider, string? friendlyName = null)
        : base(serviceProvider, friendlyName)
    {
    }

    protected override TestConsumerEndpointConfigurationBuilder<TMessage> This => this;

    protected override TestConsumerEndpointConfiguration CreateConfiguration() => new("test");
}
