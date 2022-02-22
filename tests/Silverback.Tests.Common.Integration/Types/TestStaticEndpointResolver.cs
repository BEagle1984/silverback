// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Outbound.EndpointResolvers;
using Silverback.Util;

namespace Silverback.Tests.Types;

public sealed class TestStaticEndpointResolver : StaticProducerEndpointResolver<TestProducerEndpoint, TestProducerConfiguration>
{
    public TestStaticEndpointResolver(string topic)
        : base(Check.NotNullOrEmpty(topic, nameof(topic)))
    {
        Topic = topic;
    }

    public string Topic { get; }

    protected override TestProducerEndpoint GetEndpointCore(TestProducerConfiguration configuration) =>
        new(Topic, configuration);
}
