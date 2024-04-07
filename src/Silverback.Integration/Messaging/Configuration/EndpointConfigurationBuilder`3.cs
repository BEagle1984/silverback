// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Validation;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="EndpointConfiguration" />.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages being produced or consumed.
/// </typeparam>
/// <typeparam name="TConfiguration">
///     The type of the configuration being built.
/// </typeparam>
/// <typeparam name="TBuilder">
///     The actual builder type.
/// </typeparam>
[SuppressMessage("Design", "CA1005:Avoid excessive parameters on generic types", Justification = "Not instantiated directly")]
public abstract class EndpointConfigurationBuilder<TMessage, TConfiguration, TBuilder>
    where TConfiguration : EndpointConfiguration
    where TBuilder : EndpointConfigurationBuilder<TMessage, TConfiguration, TBuilder>
{
    private readonly string? _friendlyName;

    private MessageValidationMode _messageValidationMode = MessageValidationMode.LogWarning;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EndpointConfigurationBuilder{TMessage, TConfiguration, TBuilder}" /> class.
    /// </summary>
    /// <param name="friendlyName">
    ///     An optional friendly to be shown in the human-targeted output (e.g. logs, health checks result, etc.).
    /// </param>
    protected EndpointConfigurationBuilder(string? friendlyName)
    {
        _friendlyName = friendlyName;
    }

    /// <summary>
    ///     Gets this instance.
    /// </summary>
    /// <remarks>
    ///     This is necessary to work around casting in the base classes.
    /// </remarks>
    protected abstract TBuilder This { get; }

    /// <summary>
    ///     Enables the message validation and logs a warning if the message is not valid.
    /// </summary>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder ValidateMessageAndWarn() => ValidateMessage(false);

    /// <summary>
    ///     Enables the message validation and throws an exception if the message is not valid.
    /// </summary>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder ValidateMessageAndThrow() => ValidateMessage(true);

    /// <summary>
    ///     Enables the message validation.
    /// </summary>
    /// <param name="throwException">
    ///     A value that specifies whether an exception should be thrown if the message is not valid.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder ValidateMessage(bool throwException)
    {
        _messageValidationMode = throwException ? MessageValidationMode.ThrowException : MessageValidationMode.LogWarning;
        return This;
    }

    /// <summary>
    ///     Disables the message validation.
    /// </summary>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder DisableMessageValidation()
    {
        _messageValidationMode = MessageValidationMode.None;
        return This;
    }

    /// <summary>
    ///     Builds the endpoint configuration instance.
    /// </summary>
    /// <returns>
    ///     The endpoint configuration.
    /// </returns>
    public virtual TConfiguration Build()
    {
        TConfiguration configuration = CreateConfiguration() with
        {
            FriendlyName = _friendlyName,
            MessageValidationMode = _messageValidationMode
        };

        configuration.Validate();

        return configuration;
    }

    /// <summary>
    ///     Creates the <typeparamref name="TConfiguration" /> instance.
    /// </summary>
    /// <returns>
    ///     The endpoint.
    /// </returns>
    protected abstract TConfiguration CreateConfiguration();
}
