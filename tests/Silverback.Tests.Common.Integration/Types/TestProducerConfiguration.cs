// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using NSubstitute;
using Silverback.Messaging;

namespace Silverback.Tests.Types;

public sealed record TestProducerConfiguration : ProducerConfiguration<TestProducerEndpoint>
{
    public TestProducerConfiguration()
    {
    }

    public TestProducerConfiguration(string topicName)
    {
        Endpoint = new TestStaticEndpointResolver(topicName);
    }

    public TestProducerConfiguration(params string[] topicNames)
    {
        Endpoint = new TestDynamicEndpointResolver(topicNames[0]);
    }

    [SuppressMessage("", "CA1024", Justification = "Method is appropriate (new instance)")]
    public static TestProducerConfiguration GetDefault() => new()
    {
        Endpoint = new TestStaticEndpointResolver("test")
    };

    public TestProducerEndpoint GetDefaultEndpoint() =>
        (TestProducerEndpoint)Endpoint.GetEndpoint(null, this, Substitute.For<IServiceProvider>());
}
