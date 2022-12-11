// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Messaging.Producing.EndpointResolvers;

/// <summary>
///     Dynamically resolves the target endpoint (e.g. the target topic and partition) for each message being produced.
/// </summary>
/// <typeparam name="TEndpoint">
///     The type of the endpoint being resolved.
/// </typeparam>
/// <typeparam name="TConfiguration">
///     The type of the endpoint configuration.
/// </typeparam>
public abstract record DynamicProducerEndpointResolver<TEndpoint, TConfiguration> : IDynamicProducerEndpointResolver<TEndpoint>
    where TEndpoint : ProducerEndpoint
    where TConfiguration : ProducerEndpointConfiguration
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DynamicProducerEndpointResolver{TEndpoint,TConfiguration}" /> class.
    /// </summary>
    /// <param name="rawName">
    ///     The raw endpoint name that can be used as <see cref="EndpointConfiguration.RawName" />.
    /// </param>
    protected DynamicProducerEndpointResolver(string rawName)
    {
        RawName = Check.NotNullOrEmpty(rawName, nameof(rawName));
    }

    /// <inheritdoc cref="IProducerEndpointResolver.RawName" />
    public string RawName { get; }

    /// <inheritdoc cref="IProducerEndpointResolver.GetEndpoint" />
    public ProducerEndpoint GetEndpoint(object? message, ProducerEndpointConfiguration configuration, IServiceProvider serviceProvider) =>
        GetEndpoint(message, (TConfiguration)configuration, serviceProvider);

    /// <inheritdoc cref="IProducerEndpointResolver.GetEndpoint" />
    public TEndpoint GetEndpoint(object? message, TConfiguration configuration, IServiceProvider serviceProvider) =>
        GetEndpointCore(message, configuration, serviceProvider);

    /// <inheritdoc cref="IProducerEndpointSerializer.SerializeAsync" />
    public ValueTask<byte[]> SerializeAsync(ProducerEndpoint endpoint) => SerializeAsync((TEndpoint)endpoint);

    /// <inheritdoc cref="IProducerEndpointSerializer.SerializeAsync" />
    public abstract ValueTask<byte[]> SerializeAsync(TEndpoint endpoint);

    /// <inheritdoc cref="IProducerEndpointSerializer.DeserializeAsync" />
    public async ValueTask<ProducerEndpoint> DeserializeAsync(byte[] serializedEndpoint, ProducerEndpointConfiguration configuration) =>
        await DeserializeAsync(serializedEndpoint, (TConfiguration)configuration).ConfigureAwait(false);

    /// <inheritdoc cref="IProducerEndpointSerializer.DeserializeAsync" />
    public abstract ValueTask<TEndpoint> DeserializeAsync(byte[] serializedEndpoint, TConfiguration configuration);

    /// <inheritdoc cref="IProducerEndpointResolver.GetEndpoint" />
    protected abstract TEndpoint GetEndpointCore(object? message, TConfiguration configuration, IServiceProvider serviceProvider);
}
