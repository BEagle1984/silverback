// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Validation;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     The base class for <see cref="ProducerEndpointConfiguration" /> and <see cref="ConsumerEndpointConfiguration" />.
/// </summary>
public abstract record EndpointConfiguration
{
    private readonly string _rawName = string.Empty;

    private readonly string? _friendlyName;

    /// <summary>
    ///     Gets an optional friendly name to be used to identify the endpoint. This name can be used to filter or retrieve the endpoints and
    ///     will also be included in the <see cref="DisplayName" />, to be shown in the human-targeted output (e.g. logs, health checks result, etc.).
    /// </summary>
    /// <remarks>
    ///     The <see cref="DisplayName" /> is built joining the optional <see cref="FriendlyName" /> with the <see cref="RawName" />.
    /// </remarks>
    public virtual string? FriendlyName
    {
        get => _friendlyName;
        init
        {
            _friendlyName = value;
            DisplayName = GetDisplayName();
        }
    }

    /// <summary>
    ///     Gets the raw endpoint name. This can be either the topic name for a static registration, the pattern used to create
    ///     the actual endpoint name or simply a placeholder.
    /// </summary>
    /// <remarks>
    ///     The <see cref="DisplayName" /> is built joining the optional <see cref="FriendlyName" /> with the <see cref="RawName" />.
    /// </remarks>
    public virtual string RawName
    {
        get => _rawName;
        protected init
        {
            _rawName = value;
            DisplayName = GetDisplayName();
        }
    }

    /// <summary>
    ///     Gets the name to be displayed in the human-targeted output (e.g. logs, health checks result, etc.).
    /// </summary>
    /// <remarks>
    ///     The <see cref="DisplayName" /> is built joining the optional <see cref="FriendlyName" /> with the
    ///     <see cref="RawName" />.
    /// </remarks>
    public virtual string DisplayName { get; private init; } = string.Empty;

    /// <summary>
    ///     Gets the message validation mode. This option can be used to specify if the messages have to be validated and whether an
    ///     exception must be thrown if the message is not valid. The default is <see cref="Validation.MessageValidationMode.LogWarning" />.
    /// </summary>
    public virtual MessageValidationMode MessageValidationMode { get; init; } = MessageValidationMode.LogWarning;

    /// <summary>
    ///     Validates the endpoint configuration and throws an <see cref="BrokerConfigurationException" /> if not valid.
    /// </summary>
    public void Validate()
    {
        ValidateCore();

        if (string.IsNullOrEmpty(RawName))
            throw new BrokerConfigurationException($"{nameof(RawName)} is null or empty. There could be an error in the endpoint implementation.");
    }

    /// <inheritdoc cref="Validate" />
    protected virtual void ValidateCore()
    {
    }

    private string GetDisplayName() => string.IsNullOrEmpty(FriendlyName) ? RawName : $"{FriendlyName} ({RawName})";
}
