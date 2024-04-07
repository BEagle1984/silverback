// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Producing.EndpointResolvers;

internal class NullProducerEndpointResolver : IProducerEndpointResolver
{
    public static NullProducerEndpointResolver Instance { get; } = new();

    public string RawName => string.Empty;

    public ProducerEndpoint GetEndpoint(object? message, ProducerEndpointConfiguration configuration, IServiceProvider serviceProvider) =>
        throw new NotSupportedException();

    public override string ToString() => "null";
}
