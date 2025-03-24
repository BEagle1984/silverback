// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Producing.EndpointResolvers;

internal class NullProducerEndpointResolver : IProducerEndpointResolver
{
    public static NullProducerEndpointResolver Instance { get; } = new();

    public string RawName => string.Empty;

    public ProducerEndpoint GetEndpoint(IOutboundEnvelope envelope) => throw new NotSupportedException();
}
