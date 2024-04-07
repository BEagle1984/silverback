// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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

    /// <inheritdoc cref="IProducerEndpointSerializer.Serialize" />
    public string Serialize(ProducerEndpoint endpoint) => Serialize((TEndpoint)endpoint);

    /// <inheritdoc cref="IProducerEndpointSerializer.Serialize" />
    public abstract string Serialize(TEndpoint endpoint);

    /// <inheritdoc cref="IProducerEndpointSerializer.Deserialize" />
    public ProducerEndpoint Deserialize(string serializedEndpoint, ProducerEndpointConfiguration configuration) =>
        Deserialize(serializedEndpoint, (TConfiguration)configuration);

    /// <inheritdoc cref="IProducerEndpointSerializer.Deserialize" />
    public abstract TEndpoint Deserialize(string serializedEndpoint, TConfiguration configuration);

    /// <inheritdoc cref="IProducerEndpointResolver.GetEndpoint" />
    protected abstract TEndpoint GetEndpointCore(object? message, TConfiguration configuration, IServiceProvider serviceProvider);
}
