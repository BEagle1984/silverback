// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Util;

namespace Silverback.Messaging.Producing.EndpointResolvers;

/// <summary>
///     Dynamically resolves the target topic for each message being produced.
/// </summary>
public sealed record MqttDynamicProducerEndpointResolver : DynamicProducerEndpointResolver<MqttProducerEndpoint, MqttProducerEndpointConfiguration>
{
    private readonly Func<object?, IServiceProvider, string> _topicFunction;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttDynamicProducerEndpointResolver" /> class.
    /// </summary>
    /// <param name="topicFunction">
    ///     The function returning the target topic for the message being produced.
    /// </param>
    public MqttDynamicProducerEndpointResolver(Func<object?, string> topicFunction)
        : base($"dynamic-{Guid.NewGuid():N}")
    {
        Check.NotNull(topicFunction, nameof(topicFunction));

        _topicFunction = (message, _) => topicFunction.Invoke(message);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttDynamicProducerEndpointResolver" /> class.
    /// </summary>
    /// <param name="topicFormatString">
    ///     The topic format string that will be combined with the arguments returned by the <paramref name="topicArgumentsFunction" />
    ///     using a <see cref="string.Format(string,object[])" />.
    /// </param>
    /// <param name="topicArgumentsFunction">
    ///     The function returning the arguments to be used to format the string.
    /// </param>
    [SuppressMessage("ReSharper", "CoVariantArrayConversion", Justification = "Not an issue, the array is not modified")]
    public MqttDynamicProducerEndpointResolver(string topicFormatString, Func<object?, string[]> topicArgumentsFunction)
        : base(Check.NotNullOrEmpty(topicFormatString, nameof(topicFormatString)))
    {
        Check.NotNullOrEmpty(topicFormatString, nameof(topicFormatString));
        Check.NotNull(topicArgumentsFunction, nameof(topicArgumentsFunction));

        _topicFunction = (message, _) =>
            string.Format(CultureInfo.InvariantCulture, topicFormatString, topicArgumentsFunction.Invoke(message));
    }

    internal MqttDynamicProducerEndpointResolver(Type resolverType, Func<object?, IServiceProvider, string> topicFunction)
        : base($"dynamic-{resolverType.Name}-{Guid.NewGuid():N}")
    {
        Check.NotNull(resolverType, nameof(resolverType));
        Check.NotNull(topicFunction, nameof(topicFunction));

        _topicFunction = topicFunction;
    }

    /// <inheritdoc cref="DynamicProducerEndpointResolver{TEndpoint,TConfiguration}.SerializeAsync(TEndpoint)" />
    public override ValueTask<byte[]> SerializeAsync(MqttProducerEndpoint endpoint)
    {
        Check.NotNull(endpoint, nameof(endpoint));

        return ValueTaskFactory.FromResult(Encoding.UTF8.GetBytes(endpoint.Topic));
    }

    /// <inheritdoc cref="DynamicProducerEndpointResolver{TEndpoint,TConfiguration}.DeserializeAsync(byte[],TConfiguration)" />
    public override ValueTask<MqttProducerEndpoint> DeserializeAsync(
        byte[] serializedEndpoint,
        MqttProducerEndpointConfiguration configuration)
    {
        Check.NotNull(serializedEndpoint, nameof(serializedEndpoint));
        Check.NotNull(configuration, nameof(configuration));

        string topic = Encoding.UTF8.GetString(serializedEndpoint);
        MqttProducerEndpoint endpoint = new(topic, configuration);

        return ValueTaskFactory.FromResult(endpoint);
    }

    /// <inheritdoc cref="DynamicProducerEndpointResolver{TEndpoint,TConfiguration}.GetEndpointCore" />
    protected override MqttProducerEndpoint GetEndpointCore(
        object? message,
        MqttProducerEndpointConfiguration configuration,
        IServiceProvider serviceProvider) =>
        new(_topicFunction.Invoke(message, serviceProvider), configuration);
}
