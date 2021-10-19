// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging;

namespace Silverback.Tests.Types;

public record TestConsumerEndpoint : ConsumerEndpoint<TestConsumerConfiguration>
{
    public TestConsumerEndpoint(string topic, TestConsumerConfiguration configuration)
        : base(topic, configuration)
    {
        Topic = topic;
    }

    public string Topic { get; }

    public static TestConsumerEndpoint GetDefault() => TestConsumerConfiguration.GetDefault().GetDefaultEndpoint();
}
