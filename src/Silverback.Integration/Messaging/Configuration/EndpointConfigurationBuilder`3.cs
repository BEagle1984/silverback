// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Serialization;
using Silverback.Messaging.Validation;
using Silverback.Util;

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
    private string? _friendlyName;

    private IMessageSerializer? _serializer;

    private EncryptionSettings? _encryptionSettings;

    private MessageValidationMode _messageValidationMode = MessageValidationMode.LogWarning;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EndpointConfigurationBuilder{TMessage,TEndpoint,TBuilder}" /> class.
    /// </summary>
    /// <param name="endpointsConfigurationBuilder">
    ///     The optional <see cref="EndpointsConfigurationBuilder" /> that instantiated the builder.
    /// </param>
    protected EndpointConfigurationBuilder(EndpointsConfigurationBuilder? endpointsConfigurationBuilder = null)
    {
        EndpointsConfigurationBuilder = endpointsConfigurationBuilder;
    }

    /// <summary>
    ///     Gets the <see cref="EndpointsConfigurationBuilder" /> that instantiated the builder.
    /// </summary>
    internal EndpointsConfigurationBuilder? EndpointsConfigurationBuilder { get; }

    /// <summary>
    ///     Gets the endpoint name to be displayed in the human-targeted output (e.g. logs, health checks result, etc.).
    /// </summary>
    // TODO: Get proper endpoint name from implementation
    public string EndpointDisplayName => _friendlyName ?? EndpointRawName ?? "-";

    /// <summary>
    ///     Gets the endpoint name (e.g. the topic name).
    /// </summary>
    public abstract string? EndpointRawName { get; }

    /// <summary>
    ///     Gets this instance.
    /// </summary>
    /// <remarks>
    ///     This is necessary to work around casting in the base classes.
    /// </remarks>
    protected abstract TBuilder This { get; }

    /// <summary>
    ///     Specifies an optional friendly name to be used to identify the endpoint. This name can be used to filter or retrieve the
    ///     endpoints and will also be included in the <see cref="EndpointConfiguration.DisplayName" />, to be shown in the human-targeted
    ///     output (e.g. logs, health checks result, etc.).
    /// </summary>
    /// <param name="friendlyName">
    ///     The friendly name.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder WithName(string friendlyName)
    {
        _friendlyName = Check.NotEmpty(friendlyName, nameof(friendlyName));
        return This;
    }

    /// <summary>
    ///     Specifies the <see cref="IMessageSerializer" /> to be used serialize or deserialize the messages.
    /// </summary>
    /// <param name="serializer">
    ///     The <see cref="IMessageSerializer" />.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder UseSerializer(IMessageSerializer serializer)
    {
        _serializer = Check.NotNull(serializer, nameof(serializer));
        return This;
    }

    /// <summary>
    ///     Enables the end-to-end message encryption.
    /// </summary>
    /// <param name="encryptionSettings">
    ///     The <see cref="EncryptionSettings" />.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder WithEncryption(EncryptionSettings? encryptionSettings)
    {
        _encryptionSettings = Check.NotNull(encryptionSettings, nameof(encryptionSettings));
        return This;
    }

    /// <summary>
    ///     Enables the message validation.
    /// </summary>
    /// <param name="throwException">
    ///     A value that specifies whether an exception should be thrown if the message is invalid.
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
        TConfiguration endpoint = CreateConfiguration() with
        {
            FriendlyName = _friendlyName,
            Serializer = _serializer ?? EndpointConfiguration.DefaultSerializer,
            Encryption = _encryptionSettings,
            MessageValidationMode = _messageValidationMode
        };

        endpoint.Validate();

        return endpoint;
    }

    /// <summary>
    ///     Creates the <see cref="TConfiguration"/> instance.
    /// </summary>
    /// <returns>
    ///     The endpoint.
    /// </returns>
    protected abstract TConfiguration CreateConfiguration();
}
