// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Messaging.Producing.EndpointResolvers;

/// <summary>
///     Statically resolves to the same target endpoint (e.g. the target topic and partition) for every message being produced.
/// </summary>
/// <typeparam name="TEndpoint">
///     The type of the endpoint being resolved.
/// </typeparam>
/// <typeparam name="TConfiguration">
///     The type of the endpoint configuration.
/// </typeparam>
public abstract class StaticProducerEndpointResolver<TEndpoint, TConfiguration>
    : IStaticProducerEndpointResolver<TEndpoint>, IEquatable<StaticProducerEndpointResolver<TEndpoint, TConfiguration>>
    where TEndpoint : ProducerEndpoint
    where TConfiguration : ProducerEndpointConfiguration
{
    private TEndpoint? _endpoint;

    /// <summary>
    ///     Initializes a new instance of the <see cref="StaticProducerEndpointResolver{TEndpoint, TConfiguration}" /> class.
    /// </summary>
    /// <param name="rawName">
    ///     The raw endpoint name that can be used as <see cref="EndpointConfiguration.RawName" />.
    /// </param>
    protected StaticProducerEndpointResolver(string rawName)
    {
        RawName = Check.NotNullOrEmpty(rawName, nameof(rawName));
    }

    /// <inheritdoc cref="IProducerEndpointResolver.RawName" />
    public string RawName { get; }

    /// <summary>
    ///     Equality operator.
    /// </summary>
    /// <param name="left">
    ///     Left-hand operand.
    /// </param>
    /// <param name="right">
    ///     Right-hand operand.
    /// </param>
    public static bool operator ==(
        StaticProducerEndpointResolver<TEndpoint, TConfiguration>? left,
        StaticProducerEndpointResolver<TEndpoint, TConfiguration>? right) => Equals(left, right);

    /// <summary>
    ///     Inequality operator.
    /// </summary>
    /// <param name="left">
    ///     Left-hand operand.
    /// </param>
    /// <param name="right">
    ///     Right-hand operand.
    /// </param>
    public static bool operator !=(
        StaticProducerEndpointResolver<TEndpoint, TConfiguration>? left,
        StaticProducerEndpointResolver<TEndpoint, TConfiguration>? right) => !Equals(left, right);

    /// <inheritdoc cref="IStaticProducerEndpointResolver.GetEndpoint(ProducerEndpointConfiguration)" />
    public ProducerEndpoint GetEndpoint(ProducerEndpointConfiguration configuration) =>
        GetEndpoint((TConfiguration)configuration);

    /// <inheritdoc cref="IProducerEndpointResolver.GetEndpoint" />
    public ProducerEndpoint GetEndpoint(object? message, ProducerEndpointConfiguration configuration, IServiceProvider serviceProvider) =>
        GetEndpoint((TConfiguration)configuration);

    /// <inheritdoc cref="IStaticProducerEndpointResolver.GetEndpoint(ProducerEndpointConfiguration)" />
    public TEndpoint GetEndpoint(TConfiguration configuration) =>
        _endpoint ??= GetEndpointCore(configuration);

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public virtual bool Equals(StaticProducerEndpointResolver<TEndpoint, TConfiguration>? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return RawName == other.RawName;
    }

    /// <inheritdoc cref="object.Equals(object)" />
    public sealed override bool Equals(object? obj)
    {
        if (obj is null)
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;
        return Equals((StaticProducerEndpointResolver<TEndpoint, TConfiguration>)obj);
    }

    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode() => RawName.GetHashCode(StringComparison.Ordinal);

    /// <inheritdoc cref="IStaticProducerEndpointResolver.GetEndpoint(ProducerEndpointConfiguration)" />
    /// <remarks>
    ///     This method will be called once and the result will be cached.
    /// </remarks>
    protected abstract TEndpoint GetEndpointCore(TConfiguration configuration);
}
