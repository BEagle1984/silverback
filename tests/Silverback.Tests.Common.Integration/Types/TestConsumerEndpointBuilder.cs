// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;

namespace Silverback.Tests.Types
{
    public class TestConsumerEndpointBuilder
        : ConsumerEndpointBuilder<TestConsumerEndpoint, TestConsumerEndpointBuilder>
    {
        protected override TestConsumerEndpointBuilder This => this;

        protected override TestConsumerEndpoint CreateEndpoint() => new("test");
    }
}
