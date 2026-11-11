// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using Silverback.Collections;
using Silverback.Configuration;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     The configuration used to connect with the MQTT broker. This is actually a wrapper around the
///     <see cref="MqttClientOptions" /> from the MQTTnet library.
/// </summary>
public sealed partial record MqttClientConfiguration : IValidatableSettings
{
    /// <summary>
    ///     Gets the client identifier. The default is <c>Guid.NewGuid().ToString()</c>.
    /// </summary>
    public string ClientId { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>
    ///     Gets the MQTT protocol version. The default is <see cref="MQTTnet.Formatter.MqttProtocolVersion.V500" />.
    /// </summary>
    public MqttProtocolVersion ProtocolVersion { get; init; } = MqttProtocolVersion.V500;

    /// <summary>
    ///     Gets the list of user properties to be sent with the <i>CONNECT</i> packet. They can be used to send connection related
    ///     properties from the client to the server.
    /// </summary>
    public IValueReadOnlyCollection<MqttUserProperty> UserProperties { get; init; } = ValueReadOnlyCollection.Empty<MqttUserProperty>();

    /// <summary>
    ///     Gets the channel configuration (either <see cref="MqttClientTcpConfiguration" /> or <see cref="MqttClientWebSocketConfiguration" />).
    /// </summary>
    public MqttClientChannelConfiguration? Channel { get; init; }

    /// <summary>
    ///     Gets the configured consumer endpoints.
    /// </summary>
    public IValueReadOnlyCollection<MqttConsumerEndpointConfiguration> ConsumerEndpoints { get; init; } = ValueReadOnlyCollection.Empty<MqttConsumerEndpointConfiguration>();

    /// <summary>
    ///     Gets the configured producer endpoints.
    /// </summary>
    public IValueReadOnlyCollection<MqttProducerEndpointConfiguration> ProducerEndpoints { get; init; } = ValueReadOnlyCollection.Empty<MqttProducerEndpointConfiguration>();

    /// <summary>
    ///     Gets the optional last will message to be sent when the client ungracefully disconnects.
    /// </summary>
    public MqttLastWillMessageConfiguration? WillMessage { get; init; }

    /// <summary>
    ///     Gets the maximum number of incoming messages that can be processed concurrently.
    ///     The default is 1.
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; } = 1;

    /// <summary>
    ///     Gets the maximum number of messages buffered per parallel execution, as controlled by <see cref="MaxDegreeOfParallelism" />.
    ///     Messages being processed are not included. The default is 2.
    /// </summary>
    /// <remarks>
    ///     The total buffer capacity per client is <see cref="BackpressureLimit" /> multiplied by <see cref="MaxDegreeOfParallelism" />.
    /// </remarks>
    public int BackpressureLimit { get; init; } = 2;

    /// <summary>
    ///     Gets the maximum time to wait for the acknowledgment operation to complete. The default is 30 seconds.
    /// </summary>
    public TimeSpan AcknowledgmentTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Gets a value indicating whether the headers (user properties) are supported according to the configured protocol version.
    /// </summary>
    internal bool AreHeadersSupported => ProtocolVersion >= MqttProtocolVersion.V500;

    // The batch size, if batch processing is enabled
    internal int? BatchSize => ConsumerEndpoints.FirstOrDefault(endpoint => endpoint.Batch is { Size: > 1 })?.Batch?.Size;

    // The deliveries are acknowledged only with QoS 1 or 2
    internal bool IsAcknowledgingBatches =>
        BatchSize != null && ConsumerEndpoints.Any(endpoint => endpoint.QualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce);

    // The number of deliveries that can be waiting for acknowledgment at the same time, since the batches are acknowledged when complete
    internal long MaxBatchedMessagesCount => (long)(BatchSize ?? 1) * MaxDegreeOfParallelism;

    // MQTTnet doesn't send the receive maximum if 0, and the protocol default applies
    private int EffectiveReceiveMaximum => ReceiveMaximum == 0 ? ushort.MaxValue : ReceiveMaximum;

    /// <inheritdoc cref="IValidatableSettings.Validate" />
    public void Validate()
    {
        if (ProducerEndpoints == null)
            throw new BrokerConfigurationException($"{nameof(ProducerEndpoints)} cannot be null.");

        if (ConsumerEndpoints == null)
            throw new BrokerConfigurationException($"{nameof(ConsumerEndpoints)} cannot be null.");

        if (ProducerEndpoints.Count == 0 && ConsumerEndpoints.Count == 0)
            throw new BrokerConfigurationException("At least one endpoint must be configured.");

        ProducerEndpoints.ForEach(endpoint => endpoint.Validate());
        ConsumerEndpoints.ForEach(endpoint => endpoint.Validate());
        WillMessage?.Validate();

        CheckDuplicateConsumerTopics();

        if (string.IsNullOrEmpty(ClientId))
            throw new BrokerConfigurationException($"{nameof(ClientId)} is required to connect with the message broker.");

        if (Channel == null)
            throw new BrokerConfigurationException($"{nameof(Channel)} configuration is required to connect with the message broker.");

        Channel.Validate();
        UserProperties.ForEach(property => property.Validate());
        WillMessage?.Validate();

        if (!AreHeadersSupported)
        {
            if (ConsumerEndpoints.Any(endpoint => endpoint.Deserializer.RequireHeaders))
            {
                throw new BrokerConfigurationException(
                    "Wrong deserializer configuration. Since headers (user properties) are not " +
                    "supported by MQTT prior to version 5, the deserializer must be configured with an " +
                    "hardcoded message type.");
            }

            ConsumerEndpoints.ForEach(endpoint => CheckErrorPolicyHeadersRequirement(endpoint.ErrorPolicy));
        }

        if (MaxDegreeOfParallelism < 1)
            throw new BrokerConfigurationException($"{nameof(MaxDegreeOfParallelism)} must be greater or equal to 1.");

        if (BackpressureLimit < 1)
            throw new BrokerConfigurationException($"{nameof(BackpressureLimit)} must be greater or equal to 1.");

        if (Timeout <= TimeSpan.Zero)
            throw new BrokerConfigurationException($"{nameof(Timeout)} must be greater than zero.");

        if (AcknowledgmentTimeout <= TimeSpan.Zero)
            throw new BrokerConfigurationException($"{nameof(AcknowledgmentTimeout)} must be greater than zero.");

        ValidateBatchProcessing();
        ValidateChunking();
    }

    internal MqttClientOptions GetMqttClientOptions()
    {
        MqttClientOptions options = MapCore();
        options.ClientId = ClientId;
        options.ProtocolVersion = ProtocolVersion;
        options.UserProperties = [.. UserProperties.Select(property => property.ToMqttNetType())];
        options.ChannelOptions = Channel?.ToMqttNetType();
        WillMessage?.MapToMqttNetType(options);

        return options;
    }

    private static void CheckErrorPolicyHeadersRequirement(IErrorPolicy errorPolicy)
    {
        switch (errorPolicy)
        {
            case ErrorPolicyBase errorPolicyBase:
                CheckErrorPolicyMaxFailedAttemptsNotSet(errorPolicyBase);
                break;
            case ErrorPolicyChain errorPolicyChain:
                errorPolicyChain.Policies.ForEach(CheckErrorPolicyMaxFailedAttemptsNotSet);
                break;
        }
    }

    private static void CheckErrorPolicyMaxFailedAttemptsNotSet(ErrorPolicyBase errorPolicyBase)
    {
        if (errorPolicyBase is not RetryErrorPolicy && errorPolicyBase.MaxFailedAttempts > 1)
        {
            throw new BrokerConfigurationException(
                "Cannot set MaxFailedAttempts on the error policies (except for the RetryPolicy) " +
                "because headers (user properties) are not supported by MQTT prior to version 5.");
        }
    }

    private void ValidateBatchProcessing()
    {
        if (BatchSize is not { } batchSize)
            return;

        if (!AreHeadersSupported)
            throw new BrokerConfigurationException("Batch processing requires MQTT 5.");

        // All the endpoints of the client share the same channels and sequence stores, so a batch can contain messages from different
        // endpoints (as with Kafka, when processing all partitions together)
        if (ConsumerEndpoints.Select(endpoint => endpoint.Batch).Distinct().Skip(1).Any())
        {
            throw new BrokerConfigurationException(
                "All consumer endpoints of the client must use the same batch settings. " +
                "Use separate clients for the endpoints with different batch settings.");
        }

        // The deliveries are acknowledged only when the batch is complete
        if (ConsumerEndpoints.All(endpoint => endpoint.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtMostOnce))
            return;

        // The broker stops delivering when too many messages are waiting for acknowledgment, and a broker can enforce a limit lower than
        // the receive maximum requested by the client: a batch could never be completed without a timeout
        if (ConsumerEndpoints.First().Batch?.MaxWaitTime == null)
        {
            throw new BrokerConfigurationException(
                "The batch MaxWaitTime must be set when consuming with QoS 1 or 2, since the broker stops delivering when too many " +
                "messages are waiting for acknowledgment, which could prevent the batch from being completed.");
        }

        if (MaxBatchedMessagesCount > EffectiveReceiveMaximum)
        {
            throw new BrokerConfigurationException(
                $"The batch size ({batchSize}) multiplied by the {nameof(MaxDegreeOfParallelism)} ({MaxDegreeOfParallelism}) exceeds " +
                $"the {nameof(ReceiveMaximum)} ({EffectiveReceiveMaximum}), which limits the number of messages that can be waiting " +
                $"for acknowledgment. Lower the batch size or the {nameof(MaxDegreeOfParallelism)}, or raise the {nameof(ReceiveMaximum)} " +
                "(see LimitUnacknowledgedPublications).");
        }
    }

    private void ValidateChunking()
    {
        MqttProducerEndpointConfiguration[] chunkingEndpoints =
            [.. ProducerEndpoints.Where(endpoint => endpoint.Chunk is { Size: < int.MaxValue })];

        if (chunkingEndpoints.Length == 0)
            return;

        // The chunks are correlated by the headers (user properties)
        if (!AreHeadersSupported)
            throw new BrokerConfigurationException("Chunking requires MQTT 5.");

        // The broker retains only the last message of each topic, which is a single chunk
        if (chunkingEndpoints.Any(endpoint => endpoint.Retain))
            throw new BrokerConfigurationException("Chunking can't be enabled for retained messages, since only the last chunk is retained.");
    }

    // The messages are routed by topic and the broker delivers a copy per subscription, so a filter can't be subscribed more than once,
    // not even through different share groups (the exact same topic repeated in an endpoint is a single subscription)
    private void CheckDuplicateConsumerTopics()
    {
        static string GetTopicFilter(string topic) =>
            ParsedTopic.TrySplitSharedSubscription(topic, out _, out string? filter) ? filter : topic;

        List<string> topics = [.. ConsumerEndpoints.SelectMany(endpoint => endpoint.Topics.Distinct()).Select(GetTopicFilter)];

        if (topics.Count != topics.Distinct().Count())
        {
            throw new BrokerConfigurationException(
                "Cannot connect to the same topic in different endpoints in the same consumer, or subscribe it more than once " +
                "through shared subscriptions.");
        }
    }
}
