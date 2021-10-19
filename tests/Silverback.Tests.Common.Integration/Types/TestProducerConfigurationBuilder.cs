// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;

namespace Silverback.Tests.Types
{
    public class TestProducerConfigurationBuilder<TMessage>
        : ProducerConfigurationBuilder<TMessage, TestProducerConfiguration, TestProducerEndpoint, TestProducerConfigurationBuilder<TMessage>>
    {
        private string _topicName = "test";

        public override string EndpointRawName => _topicName;

        protected override TestProducerConfigurationBuilder<TMessage> This => this;

        public TestProducerConfigurationBuilder<TMessage> ProduceTo(string topicName)
        {
            _topicName = topicName;
            return this;
        }

        protected override TestProducerConfiguration CreateConfiguration() => new()
        {
            Endpoint = new TestStaticEndpointResolver(_topicName)
        };
    }
}
