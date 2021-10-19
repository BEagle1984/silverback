// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Outbound.EndpointResolvers;

internal class NullProducerEndpointResolver : IProducerEndpointResolver
{
    public static NullProducerEndpointResolver Instance { get; } = new();

    public string RawName => throw new NotSupportedException();

    public ProducerEndpoint GetEndpoint(object? message, ProducerConfiguration configuration, IServiceProvider serviceProvider) =>
        throw new NotSupportedException();
}
