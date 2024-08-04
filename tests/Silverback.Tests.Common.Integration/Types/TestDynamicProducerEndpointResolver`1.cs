// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Util;

namespace Silverback.Tests.Types;

public sealed record TestDynamicProducerEndpointResolver<TMessage> : DynamicProducerEndpointResolver<TMessage, TestProducerEndpoint, TestProducerEndpointConfiguration>
    where TMessage : class
{
    private readonly string _topicName;

    public TestDynamicProducerEndpointResolver(string topic)
        : base($"dynamic-{Guid.NewGuid():N}")
    {
        _topicName = Check.NotNullOrEmpty(topic, nameof(topic));
    }

    public override string Serialize(TestProducerEndpoint endpoint) => throw new NotSupportedException();

    public override TestProducerEndpoint Deserialize(string serializedEndpoint, TestProducerEndpointConfiguration configuration) =>
        throw new NotSupportedException();

    protected override TestProducerEndpoint GetEndpointCore(
        TMessage? message,
        TestProducerEndpointConfiguration configuration,
        IServiceProvider serviceProvider) =>
        new(_topicName, configuration);
}
