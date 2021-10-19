// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Outbound.EndpointResolvers;
using Silverback.Util;

namespace Silverback.Tests.Types;

public sealed record TestDynamicEndpointResolver : DynamicProducerEndpointResolver<TestProducerEndpoint, TestProducerConfiguration>
{
    private readonly string _topicName;

    public TestDynamicEndpointResolver(string topicName)
        : base($"dynamic-{Guid.NewGuid():N}")
    {
        _topicName = Check.NotEmpty(topicName, nameof(topicName));
    }

    public override ValueTask<TestProducerEndpoint> DeserializeAsync(
        byte[] serializedEndpoint,
        TestProducerConfiguration endpointConfiguration) =>
        throw new NotImplementedException();

    public override ValueTask<byte[]> SerializeAsync(TestProducerEndpoint endpoint) =>
        throw new NotImplementedException();

    protected override TestProducerEndpoint GetEndpointCore(
        object? message,
        TestProducerConfiguration endpointConfiguration,
        IServiceProvider serviceProvider) =>
        new(_topicName, endpointConfiguration);
}
