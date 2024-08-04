// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Util;

namespace Silverback.Messaging.Producing.EndpointResolvers;

/// <summary>
///     Dynamically resolves the target topic for each message being produced.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the message being produced.
/// </typeparam>
public sealed record MqttDynamicProducerEndpointResolver<TMessage>
    : DynamicProducerEndpointResolver<TMessage, MqttProducerEndpoint, MqttProducerEndpointConfiguration>
    where TMessage : class
{
    private readonly Func<TMessage?, IServiceProvider, string> _topicFunction;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttDynamicProducerEndpointResolver{TMessage}" /> class.
    /// </summary>
    /// <param name="topicFunction">
    ///     The function returning the target topic for the message being produced.
    /// </param>
    public MqttDynamicProducerEndpointResolver(Func<TMessage?, string> topicFunction)
        : base($"dynamic-{Guid.NewGuid():N}")
    {
        Check.NotNull(topicFunction, nameof(topicFunction));

        _topicFunction = (message, _) => topicFunction.Invoke(message);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttDynamicProducerEndpointResolver{TMessage}" /> class.
    /// </summary>
    /// <param name="topicFormatString">
    ///     The topic format string that will be combined with the arguments returned by the <paramref name="topicArgumentsFunction" />
    ///     using a <see cref="string.Format(string,object[])" />.
    /// </param>
    /// <param name="topicArgumentsFunction">
    ///     The function returning the arguments to be used to format the string.
    /// </param>
    [SuppressMessage("ReSharper", "CoVariantArrayConversion", Justification = "Not an issue, the array is not modified")]
    public MqttDynamicProducerEndpointResolver(string topicFormatString, Func<TMessage?, string[]> topicArgumentsFunction)
        : base(Check.NotNullOrEmpty(topicFormatString, nameof(topicFormatString)))
    {
        Check.NotNullOrEmpty(topicFormatString, nameof(topicFormatString));
        Check.NotNull(topicArgumentsFunction, nameof(topicArgumentsFunction));

        _topicFunction = (message, _) =>
            string.Format(CultureInfo.InvariantCulture, topicFormatString, topicArgumentsFunction.Invoke(message));
    }

    internal MqttDynamicProducerEndpointResolver(Type resolverType, Func<TMessage?, IServiceProvider, string> topicFunction)
        : base($"dynamic-{resolverType.Name}-{Guid.NewGuid():N}")
    {
        Check.NotNull(resolverType, nameof(resolverType));
        Check.NotNull(topicFunction, nameof(topicFunction));

        _topicFunction = topicFunction;
    }

    /// <inheritdoc cref="DynamicProducerEndpointResolver{TMessage,TEndpoint,TConfiguration}.Serialize(TEndpoint)" />
    public override string Serialize(MqttProducerEndpoint endpoint)
    {
        Check.NotNull(endpoint, nameof(endpoint));

        return endpoint.Topic;
    }

    /// <inheritdoc cref="DynamicProducerEndpointResolver{TMessage,TEndpoint,TConfiguration}.Deserialize(string,TConfiguration)" />
    public override MqttProducerEndpoint Deserialize(
        string serializedEndpoint,
        MqttProducerEndpointConfiguration configuration)
    {
        Check.NotNull(serializedEndpoint, nameof(serializedEndpoint));
        Check.NotNull(configuration, nameof(configuration));

        return new MqttProducerEndpoint(serializedEndpoint, configuration);
    }

    /// <inheritdoc cref="DynamicProducerEndpointResolver{TMessage,TEndpoint,TConfiguration}.GetEndpointCore" />
    protected override MqttProducerEndpoint GetEndpointCore(
        TMessage? message,
        MqttProducerEndpointConfiguration configuration,
        IServiceProvider serviceProvider) =>
        new(_topicFunction.Invoke(message, serviceProvider), configuration);
}
