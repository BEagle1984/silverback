// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Outbound.EndpointResolvers;
using Silverback.Util;

namespace Silverback.Tests.Integration.TestTypes;

public sealed class TestOtherStaticEndpointResolver
    : StaticProducerEndpointResolver<TestOtherProducerEndpoint, TestOtherProducerConfiguration>
{
    public TestOtherStaticEndpointResolver(string topic)
        : base(Check.NotEmpty(topic, nameof(topic)))
    {
        Topic = topic;
    }

    public string Topic { get; }

    protected override TestOtherProducerEndpoint GetEndpointCore(TestOtherProducerConfiguration configuration) => new(Topic, configuration);
}
