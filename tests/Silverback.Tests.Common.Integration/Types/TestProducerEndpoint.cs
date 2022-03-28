// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging;

namespace Silverback.Tests.Types;

public record TestProducerEndpoint : ProducerEndpoint<TestProducerEndpointConfiguration>
{
    public TestProducerEndpoint(string topic, TestProducerEndpointConfiguration configuration)
        : base(topic, configuration)
    {
        Topic = topic;
    }

    public string Topic { get; }

    public static TestProducerEndpoint GetDefault() => TestProducerEndpointConfiguration.GetDefault().GetDefaultEndpoint();
}
