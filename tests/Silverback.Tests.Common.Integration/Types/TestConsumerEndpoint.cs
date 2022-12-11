// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging;

namespace Silverback.Tests.Types;

public record TestConsumerEndpoint : ConsumerEndpoint<TestConsumerEndpointConfiguration>
{
    public TestConsumerEndpoint(string topic, TestConsumerEndpointConfiguration configuration)
        : base(topic, configuration)
    {
        Topic = topic;
    }

    public string Topic { get; }

    public static TestConsumerEndpoint GetDefault() => TestConsumerEndpointConfiguration.GetDefault().GetDefaultEndpoint();
}
