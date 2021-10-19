// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.EndpointResolvers;

/// <summary>
///     Dynamically resolves the target endpoint (e.g. the target topic and partition) for each message being produced.
/// </summary>
/// <typeparam name="TEndpoint">
///     The type of the endpoint being resolved.
/// </typeparam>
/// <typeparam name="TConfiguration">
///     The type of the endpoint configuration.
/// </typeparam>
public abstract record DynamicProducerEndpointResolver<TEndpoint, TConfiguration>
    : IDynamicProducerEndpointResolver<TEndpoint>
    where TEndpoint : ProducerEndpoint
    where TConfiguration : ProducerConfiguration
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DynamicProducerEndpointResolver{TEndpoint,TConfiguration}" /> class.
    /// </summary>
    /// <param name="rawName">
    ///     The raw endpoint name that can be used as <see cref="ProducerConfiguration.RawName" />.
    /// </param>
    protected DynamicProducerEndpointResolver(string rawName)
    {
        RawName = Check.NotEmpty(rawName, nameof(rawName));
    }

    /// <inheritdoc cref="IProducerEndpointResolver.RawName" />
    public string RawName { get; }

    /// <inheritdoc cref="IProducerEndpointResolver.GetEndpoint" />
    public ProducerEndpoint GetEndpoint(object? message, ProducerConfiguration configuration, IServiceProvider serviceProvider) =>
        GetEndpoint(message, (TConfiguration)configuration, serviceProvider);

    /// <inheritdoc cref="IProducerEndpointResolver.GetEndpoint" />
    public TEndpoint GetEndpoint(object? message, TConfiguration configuration, IServiceProvider serviceProvider) =>
        GetEndpointCore(message, configuration, serviceProvider);

    /// <inheritdoc cref="IProducerEndpointSerializer.SerializeAsync" />
    public ValueTask<byte[]> SerializeAsync(ProducerEndpoint endpoint) => SerializeAsync((TEndpoint)endpoint);

    /// <inheritdoc cref="IProducerEndpointSerializer.SerializeAsync" />
    public abstract ValueTask<byte[]> SerializeAsync(TEndpoint endpoint);

    /// <inheritdoc cref="IProducerEndpointSerializer.DeserializeAsync" />
    public async ValueTask<ProducerEndpoint> DeserializeAsync(byte[] serializedEndpoint, ProducerConfiguration configuration) =>
        await DeserializeAsync(serializedEndpoint, (TConfiguration)configuration).ConfigureAwait(false);

    /// <inheritdoc cref="IProducerEndpointSerializer.DeserializeAsync" />
    public abstract ValueTask<TEndpoint> DeserializeAsync(byte[] serializedEndpoint, TConfiguration configuration);

    /// <inheritdoc cref="IProducerEndpointResolver.GetEndpoint" />
    protected abstract TEndpoint GetEndpointCore(object? message, TConfiguration configuration, IServiceProvider serviceProvider);
}
