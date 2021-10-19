// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using NSubstitute;
using Silverback.Messaging;

namespace Silverback.Tests.Integration.TestTypes;

public sealed record TestOtherProducerConfiguration : ProducerConfiguration<TestOtherProducerEndpoint>
{
    public TestOtherProducerConfiguration()
    {
    }

    public TestOtherProducerConfiguration(string topicName)
    {
        Endpoint = new TestOtherStaticEndpointResolver(topicName);
    }

    [SuppressMessage("", "CA1024", Justification = "Method is appropriate (new instance)")]
    public static TestOtherProducerConfiguration GetDefault() => new()
    {
        Endpoint = new TestOtherStaticEndpointResolver("test")
    };

    [SuppressMessage("", "CA1024", Justification = "Method is appropriate (new instance)")]
    public TestOtherProducerEndpoint GetDefaultEndpoint() =>
        (TestOtherProducerEndpoint)Endpoint.GetEndpoint(null, this, Substitute.For<IServiceProvider>());
}
