// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;

namespace Silverback.Tests.Types
{
    public class TestProducerEndpointBuilder
        : ProducerEndpointBuilder<TestProducerEndpoint, TestProducerEndpointBuilder>
    {
        protected override TestProducerEndpointBuilder This => this;

        protected override TestProducerEndpoint CreateEndpoint() => new("test");
    }
}
