// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging;

namespace Silverback.Tests.Integration.TestTypes;

public record TestActualOtherConsumerEndpoint : ConsumerEndpoint<TestOtherConsumerConfiguration>
{
    public TestActualOtherConsumerEndpoint(string topic, TestOtherConsumerConfiguration configuration)
        : base(topic, configuration)
    {
        Topic = topic;
    }

    public string Topic { get; }
}
