// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Producing.EndpointResolvers;

internal class NullProducerEndpointResolver<TEndpoint> : NullProducerEndpointResolver, IProducerEndpointResolver<TEndpoint>
    where TEndpoint : ProducerEndpoint
{
    public static new readonly NullProducerEndpointResolver<TEndpoint> Instance = new();

    private NullProducerEndpointResolver()
    {
    }
}