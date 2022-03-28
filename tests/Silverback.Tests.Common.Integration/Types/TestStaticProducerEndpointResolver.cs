// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Util;

namespace Silverback.Tests.Types;

public sealed class TestStaticProducerEndpointResolver
    : StaticProducerEndpointResolver<TestProducerEndpoint, TestProducerEndpointConfiguration>
{
    public TestStaticProducerEndpointResolver(string topic)
        : base(Check.NotNullOrEmpty(topic, nameof(topic)))
    {
        Topic = topic;
    }

    public string Topic { get; }

    protected override TestProducerEndpoint GetEndpointCore(TestProducerEndpointConfiguration configuration) =>
        new(Topic, configuration);
}
