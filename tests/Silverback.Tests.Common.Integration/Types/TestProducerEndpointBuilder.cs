// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;

namespace Silverback.Tests.Types
{
    public class TestProducerEndpointBuilder
        : ProducerEndpointBuilder<TestProducerEndpoint, TestProducerEndpointBuilder>
    {
        private string _topicName = "test";

        protected override TestProducerEndpointBuilder This => this;

        public TestProducerEndpointBuilder ProduceTo(string topicName)
        {
            _topicName = topicName;
            return this;
        }

        protected override TestProducerEndpoint CreateEndpoint() => new(_topicName);
    }
}
