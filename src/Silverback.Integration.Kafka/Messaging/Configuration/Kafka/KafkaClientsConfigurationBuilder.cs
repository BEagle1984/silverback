// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Configures the Kafka producers and consumers building the <see cref="KafkaProducerConfiguration" /> and
///     <see cref="KafkaConsumerConfiguration" />.
/// </summary>
/// TODO: Test idempotency (e.g. AddOrMergeProducer should distinct on message type and topic)
public partial class KafkaClientsConfigurationBuilder
{
    private readonly KafkaClientsConfigurationActions _configurationActions = new();

    private readonly List<Action<IKafkaClientConfigurationBuilder>> _sharedConfigurationActions = [];

    /// <summary>
    ///     Adds a Kafka producer.
    /// </summary>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="KafkaProducerConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaClientsConfigurationBuilder AddProducer(Action<KafkaProducerConfigurationBuilder> configurationBuilderAction) =>
        AddProducer(Guid.NewGuid().ToString(), configurationBuilderAction);

    /// <summary>
    ///     Adds a Kafka producer or updates its configuration if a producer with the same name already exists.
    /// </summary>
    /// <param name="name">
    ///     The producer name.
    /// </param>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="KafkaProducerConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaClientsConfigurationBuilder AddProducer(string name, Action<KafkaProducerConfigurationBuilder> configurationBuilderAction)
    {
        Check.NotNullOrEmpty(name, nameof(name));
        Check.NotNull(configurationBuilderAction, nameof(configurationBuilderAction));

        _configurationActions.ProducerConfigurationActions.AddOrAppend(name, configurationBuilderAction);

        return this;
    }

    /// <summary>
    ///     Adds a Kafka consumer.
    /// </summary>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="KafkaConsumerConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaClientsConfigurationBuilder AddConsumer(Action<KafkaConsumerConfigurationBuilder> configurationBuilderAction) =>
        AddConsumer(Guid.NewGuid().ToString(), configurationBuilderAction);

    /// <summary>
    ///     Adds a Kafka consumer or updates its configuration if a consumer with the same name already exists.
    /// </summary>
    /// <param name="name">
    ///     The consumer name.
    /// </param>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="KafkaConsumerConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaClientsConfigurationBuilder AddConsumer(string name, Action<KafkaConsumerConfigurationBuilder> configurationBuilderAction)
    {
        Check.NotNullOrEmpty(name, nameof(name));
        Check.NotNull(configurationBuilderAction, nameof(configurationBuilderAction));

        _configurationActions.ConsumerConfigurationActions.AddOrAppend(name, configurationBuilderAction);

        return this;
    }

    /// <summary>
    ///     Sets the SASL mechanism to use for authentication. Supported: GSSAPI, PLAIN, SCRAM-SHA-256, SCRAM-SHA-512.
    /// </summary>
    /// <param name="saslMechanism">
    ///     The SASL mechanism.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSaslMechanism(SaslMechanism? saslMechanism);

    /// <summary>
    ///     Sets the number of acknowledgements that the leader broker must receive from the in-sync replicas before responding to the request:
    ///     <see cref="Acks.None" /> (no response/ack is sent to the client), <see cref="Acks.Leader" /> (the leader will write the record to
    ///     its local log but will respond without awaiting full acknowledgement from all followers, or <see cref="Acks.All" /> (the broker
    ///     will block until the message is committed by all in-sync replicas. If there are less than <c>min.insync.replicas</c> (broker configuration)
    ///     in the in-sync replicas set the produce request will fail.
    /// </summary>
    /// <param name="acks">
    ///     The number of acknowledgements that the leader broker must receive from the in-sync replicas.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithAcks(Acks? acks);

    /// <summary>
    ///     Sets the comma-separated list of brokers (host or host:port).
    /// </summary>
    /// <param name="bootstrapServers">
    ///     The comma-separated list of brokers.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithBootstrapServers(string? bootstrapServers);

    /// <summary>
    ///     Sets the maximum message size.
    /// </summary>
    /// <param name="messageMaxBytes">
    ///     The maximum message size.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithMessageMaxBytes(int? messageMaxBytes);

    /// <summary>
    ///     Sets the maximum size for a message to be copied into the buffer. Messages larger than this will be passed by reference (zero-copy)
    ///     at the expense of larger iovecs.
    /// </summary>
    /// <param name="messageCopyMaxBytes">
    ///     The maximum size for a message to be copied into the buffer.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithMessageCopyMaxBytes(int? messageCopyMaxBytes);

    /// <summary>
    ///     Sets the maximum response message size. This serves as a safety precaution to avoid memory exhaustion in case of protocol hickups.
    ///     This value must be at least <see cref="KafkaConsumerConfiguration.FetchMaxBytes" /> + 512 to allow for protocol overhead.
    ///     The value is adjusted automatically unless the configuration property is explicitly set.
    /// </summary>
    /// <param name="receiveMessageMaxBytes">
    ///     The maximum response message size.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithReceiveMessageMaxBytes(int? receiveMessageMaxBytes);

    /// <summary>
    ///     Sets the maximum number of in-flight requests per broker connection. This is a generic property applied to all broker communication,
    ///     however it is primarily relevant to produce requests. In particular, note that other mechanisms limit the number of outstanding consumer
    ///     fetch request per broker to one.
    /// </summary>
    /// <param name="maxInFlight">
    ///     The maximum number of in-flight requests per broker connection.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithMaxInFlight(int? maxInFlight);

    /// <summary>
    ///     Sets the interval (in milliseconds) at which the topic and broker metadata is refreshed in order to proactively discover any new
    ///     brokers, topics, partitions or partition leader changes. Use -1 to disable the intervalled refresh (not recommended). If there are
    ///     no locally referenced topics (no topic objects created, no messages produced, no subscription or no assignment) then only the broker
    ///     list will be refreshed every interval but no more often than every 10s.
    /// </summary>
    /// <param name="topicMetadataRefreshIntervalMs">
    ///     The interval at which the topic and broker metadata is refreshed.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithTopicMetadataRefreshIntervalMs(int? topicMetadataRefreshIntervalMs);

    /// <summary>
    ///     Sets the metadata cache max age (in milliseconds). Defaults to <see cref="KafkaClientConfiguration{TClientConfig}.TopicMetadataRefreshIntervalMs" />.
    /// </summary>
    /// <param name="metadataMaxAgeMs">
    ///     The metadata cache max age.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithMetadataMaxAgeMs(int? metadataMaxAgeMs);

    /// <summary>
    ///     Sets the refresh interval (in milliseconds) to be applied instead of the <see cref="KafkaClientConfiguration{TClientConfig}.TopicMetadataRefreshIntervalMs" />
    ///     when a topic loses its leader and a new metadata request will be enqueued. This initial interval will be exponentially increased
    ///     until the topic metadata has been refreshed. This is used to recover quickly from transitioning leader brokers.
    /// </summary>
    /// <param name="topicMetadataRefreshFastIntervalMs">
    ///     The refresh interval.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithTopicMetadataRefreshFastIntervalMs(int? topicMetadataRefreshFastIntervalMs);

    /// <summary>
    ///     Enables Generates less topic metadata requests (consuming less network bandwidth).
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaClientsConfigurationBuilder EnableSparseTopicMetadataRefresh()
    {
        WithTopicMetadataRefreshSparse(true);
        return this;
    }

    /// <summary>
    ///     Disables sparse topic metadata requests.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaClientsConfigurationBuilder DisableSparseTopicMetadataRefresh()
    {
        WithTopicMetadataRefreshSparse(false);
        return this;
    }

    /// <summary>
    ///     Sets the delay (in milliseconds) to be applied before marking a topic as non-existent. The maximum propagation time is calculated
    ///     from the time the topic is first referenced in the client.
    /// </summary>
    /// <param name="topicMetadataPropagationMaxMs">
    ///     The the delay to be observed before marking a topic as non-existent.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithTopicMetadataPropagationMaxMs(int? topicMetadataPropagationMaxMs);

    /// <summary>
    ///     Sets a comma-separated list of regular expressions for matching topic names that should be ignored in broker metadata information
    ///     as if the topics did not exist.
    /// </summary>
    /// <param name="topicBlacklist">
    ///     The comma-separated list of regular expressions for the topic black list.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithTopicBlacklist(string? topicBlacklist);

    /// <summary>
    ///     Sets a comma-separated list of debug contexts to enable.
    ///     Detailed producer debugging: <c>broker,topic,msg</c>.
    ///     Detailed consumer debugging: <c>consumer,cgrp,topic,fetch</c>.
    /// </summary>
    /// <param name="debug">
    ///     The comma-separated list of debug contexts to enable.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithDebug(string? debug);

    /// <summary>
    ///     Sets the default timeout (in milliseconds) for network requests.
    /// </summary>
    /// <param name="socketTimeoutMs">
    ///     The default timeout for network requests.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSocketTimeoutMs(int? socketTimeoutMs);

    /// <summary>
    ///     Sets the socket send buffer size. The system default is used if 0.
    /// </summary>
    /// <param name="socketSendBufferBytes">
    ///     The socket send buffer size.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSocketSendBufferBytes(int? socketSendBufferBytes);

    /// <summary>
    ///     Sets the socket receive buffer size. The system default is used if 0.
    /// </summary>
    /// <param name="socketReceiveBufferBytes">
    ///     The socket receive buffer size.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSocketReceiveBufferBytes(int? socketReceiveBufferBytes);

    /// <summary>
    ///     Enables the TCP keep-alive (SO_KEEPALIVE) on the broker sockets.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaClientsConfigurationBuilder EnableSocketKeepalive()
    {
        WithSocketKeepaliveEnable(true);
        return this;
    }

    /// <summary>
    ///     Disables the TCP keep-alive (SO_KEEPALIVE) on the broker sockets.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaClientsConfigurationBuilder DisableSocketKeepalive()
    {
        WithSocketKeepaliveEnable(false);
        return this;
    }

    /// <summary>
    ///     Disables the Nagle's algorithm (TCP_NODELAY) on the broker sockets.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaClientsConfigurationBuilder DisableSocketNagle()
    {
        WithSocketNagleDisable(true);
        return this;
    }

    /// <summary>
    ///     Enables the Nagle's algorithm (TCP_NODELAY) on the broker sockets.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaClientsConfigurationBuilder EnableSocketNagle()
    {
        WithSocketNagleDisable(false);
        return this;
    }

    /// <summary>
    ///     Sets the maximum number of send failures (e.g. timed out requests) before disconnecting. Disable with 0.<br />
    ///     Warning: It is highly recommended to leave this setting at its default value of 1 to avoid the client and broker to
    ///     become desynchronized in case of request timeouts.<br />
    ///     Note: The connection is automatically re-established.
    /// </summary>
    /// <param name="socketMaxFails">
    ///     The maximum number of send failures before disconnecting.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSocketMaxFails(int? socketMaxFails);

    /// <summary>
    ///     Sets the duration in milliseconds of the cache of the broker address resolving results.
    /// </summary>
    /// <param name="brokerAddressTtl">
    ///     The duration in milliseconds of the cache of the broker address resolving results.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithBrokerAddressTtl(int? brokerAddressTtl);

    /// <summary>
    ///     Sets the allowed broker IP address families.
    /// </summary>
    /// <param name="brokerAddressFamily">
    ///     The allowed broker IP address families.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithBrokerAddressFamily(BrokerAddressFamily? brokerAddressFamily);

    /// <summary>
    ///     Sets the maximum time (in milliseconds) allowed for the setup of the broker connection (TCP connection setup and SSL/SASL handshake).
    ///     The connection to the broker will be closed and retried, if the timeout elapses before it is fully functional.
    /// </summary>
    /// <param name="socketConnectionSetupTimeoutMs">
    ///     The maximum time allowed for the setup of the broker connection.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSocketConnectionSetupTimeoutMs(int? socketConnectionSetupTimeoutMs);

    /// <summary>
    ///     Sets the maximum time of inactivity (in milliseconds) before closing the broker connections. Disable with 0.
    ///     If this property is left at its default value some heuristics are performed to determine a suitable default value.
    /// </summary>
    /// <param name="connectionsMaxIdleMs">
    ///     The maximum time of inactivity.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithConnectionsMaxIdleMs(int? connectionsMaxIdleMs);

    /// <summary>
    ///     Sets the initial time (in milliseconds) to wait before reconnecting to a broker after the connection has been closed. The time is increased exponentially
    ///     until <see cref="KafkaClientConfiguration{TClientConfig}.ReconnectBackoffMaxMs" /> is reached. -25% to +50% jitter is applied to each reconnect backoff.
    ///     A value of 0 disables the backoff and reconnects immediately.
    /// </summary>
    /// <param name="reconnectBackoffMs">
    ///     The initial time to wait before reconnecting to a broker.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithReconnectBackoffMs(int? reconnectBackoffMs);

    /// <summary>
    ///     Sets the maximum time (in milliseconds) to wait before reconnecting to a broker after the connection has been closed.
    /// </summary>
    /// <param name="reconnectBackoffMaxMs">
    ///     The maximum time to wait before reconnecting to a broker.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithReconnectBackoffMaxMs(int? reconnectBackoffMaxMs);

    /// <summary>
    ///     Sets the statistics emit interval (in milliseconds). The granularity is 1000ms. A value of 0 disables statistics.
    /// </summary>
    /// <param name="statisticsIntervalMs">
    ///     The statistics emit interval.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithStatisticsIntervalMs(int? statisticsIntervalMs);

    /// <summary>
    ///     Enables the API versions requests to adjust the functionality according to the available protocol features.
    ///     If the request fails, the fallback version specified in <see cref="KafkaClientConfiguration{TClientConfig}.BrokerVersionFallback" /> will be used.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaClientsConfigurationBuilder EnableApiVersionRequest()
    {
        WithApiVersionRequest(true);
        return this;
    }

    /// <summary>
    ///     Disables the API versions requests and uses the fallback version specified in <see cref="KafkaClientConfiguration{TClientConfig}.BrokerVersionFallback" />.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaClientsConfigurationBuilder DisableApiVersionRequest()
    {
        WithApiVersionRequest(false);
        return this;
    }

    /// <summary>
    ///     Sets the timeout (in milliseconds) for the broker API version requests.
    /// </summary>
    /// <param name="apiVersionRequestTimeoutMs">
    ///     The timeout for the broker API version requests.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithApiVersionRequestTimeoutMs(int? apiVersionRequestTimeoutMs);

    /// <summary>
    ///     Sets how long the <see cref="KafkaClientConfiguration{TClientConfig}.BrokerVersionFallback" /> is used in the case the API version request fails.
    /// </summary>
    /// <param name="apiVersionFallbackMs">
    ///     How long the fallback API version must be used.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithApiVersionFallbackMs(int? apiVersionFallbackMs);

    /// <summary>
    ///     Sets the broker API version to be used when the API version request fails or it's disabled. Older broker versions (before 0.10.0) don't support
    ///     the API version request. Valid values are: 0.9.0, 0.8.2, 0.8.1, 0.8.0. Any other value &gt;= 0.10, such as 0.10.2.1, enables the <see cref="KafkaClientConfiguration{TClientConfig}.ApiVersionRequest" />.
    /// </summary>
    /// <param name="brokerVersionFallback">
    ///     The broker API version to be used as fallback.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithBrokerVersionFallback(string? brokerVersionFallback);

    /// <summary>
    ///     Sets the protocol to be used to communicate with the brokers.
    /// </summary>
    /// <param name="securityProtocol">
    ///     The protocol to be used to communicate with the brokers.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSecurityProtocol(SecurityProtocol? securityProtocol);

    /// <summary>
    ///     Sets the SSL cipher suites.
    /// </summary>
    /// <param name="sslCipherSuites">
    ///     The SSL cipher suites.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSslCipherSuites(string? sslCipherSuites);

    /// <summary>
    ///     Sets the supported SSL curves.
    /// </summary>
    /// <param name="sslCurvesList">
    ///     The supported SSL curves.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSslCurvesList(string? sslCurvesList);

    /// <summary>
    ///     Sets the supported SSL signature algorithms.
    /// </summary>
    /// <param name="sslSigalgsList">
    ///     The supported SSL signature algorithms.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSslSigalgsList(string? sslSigalgsList);

    /// <summary>
    ///     Sets the path to the client's private key (PEM) used for the authentication.
    /// </summary>
    /// <param name="sslKeyLocation">
    ///     The path to the client's private key (PEM).
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSslKeyLocation(string? sslKeyLocation);

    /// <summary>
    ///     Sets the private key passphrase.
    /// </summary>
    /// <param name="sslKeyPassword">
    ///     The private key passphrase.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSslKeyPassword(string? sslKeyPassword);

    /// <summary>
    ///     Sets the client's private key string (in PEM format) used for the authentication.
    /// </summary>
    /// <param name="sslKeyPem">
    ///     The client's private key string (in PEM format).
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSslKeyPem(string? sslKeyPem);

    /// <summary>
    ///     Sets the path to the client's public key (PEM) used for the authentication.
    /// </summary>
    /// <param name="sslCertificateLocation">
    ///     The path to the client's public key (PEM).
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSslCertificateLocation(string? sslCertificateLocation);

    /// <summary>
    ///     Sets the client's public key string (in PEM format) used for the authentication.
    /// </summary>
    /// <param name="sslCertificatePem">
    ///     The client's public key string (in PEM format).
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSslCertificatePem(string? sslCertificatePem);

    /// <summary>
    ///     Sets the file or directory path to the CA certificate(s) for verifying the broker's key. Defaults: On Windows the system's CA certificates are automatically looked up in the Windows Root certificate store.
    ///     On Mac OSX this configuration defaults to <c>probe</c>. It is recommended to install openssl using Homebrew, to provide CA certificates. On Linux install the distribution's ca-certificates package.
    ///     If OpenSSL is statically linked or <see cref="KafkaClientConfiguration{TClientConfig}.SslCaLocation" /> is set to <c>probe</c> a list of standard paths will be probed and the first one found will be used as the default CA certificate location path.
    ///     If OpenSSL is dynamically linked the OpenSSL library's default path will be used (see <c>OPENSSLDIR</c> in <c>openssl version -a</c>).
    /// </summary>
    /// <param name="sslCaLocation">
    ///     The file or directory path to the CA certificate(s) for verifying the broker's key.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSslCaLocation(string? sslCaLocation);

    /// <summary>
    ///     Sets the CA certificate string (in PEM format) for verifying the broker's key.
    /// </summary>
    /// <param name="sslCaPem">
    ///     The CA certificate string (in PEM format).
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSslCaPem(string? sslCaPem);

    /// <summary>
    ///     Sets a comma-separated list of Windows certificate stores to load CA certificates from. The certificates will be loaded in the same order
    ///     as stores are specified. If no certificates can be loaded from any of the specified stores an error is logged and the OpenSSL library's default
    ///     CA location is used instead. Store names are typically one or more of: MY, Root, Trust, CA.
    /// </summary>
    /// <param name="sslCaCertificateStores">
    ///     A comma-separated list of Windows certificate stores to load CA certificates from.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSslCaCertificateStores(string? sslCaCertificateStores);

    /// <summary>
    ///     Sets the path to the certificate revocation list (CRL) for verifying broker's certificate validity.
    /// </summary>
    /// <param name="sslCrlLocation">
    ///     The path to the certificate revocation list (CRL).
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSslCrlLocation(string? sslCrlLocation);

    /// <summary>
    ///     Sets the path to the client's keystore (PKCS#12) used for the authentication.
    /// </summary>
    /// <param name="sslKeystoreLocation">
    ///     The path to the client's keystore (PKCS#12).
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSslKeystoreLocation(string? sslKeystoreLocation);

    /// <summary>
    ///     Sets the client's keystore (PKCS#12) password.
    /// </summary>
    /// <param name="sslKeystorePassword">
    ///     The client's keystore (PKCS#12) password.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSslKeystorePassword(string? sslKeystorePassword);

    /// <summary>
    ///     Sets the comma-separated list of OpenSSL 3.0.x implementation providers.
    /// </summary>
    /// <param name="sslProviders">
    ///     The comma-separated list of OpenSSL 3.0.x implementation providers.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSslProviders(string? sslProviders);

    /// <summary>
    ///     Sets the path to the OpenSSL engine library. OpenSSL &gt;= 1.1.0 required.
    /// </summary>
    /// <param name="sslEngineLocation">
    ///     The path to the OpenSSL engine library.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSslEngineLocation(string? sslEngineLocation);

    /// <summary>
    ///     Sets the OpenSSL engine id (the name used for loading engine).
    /// </summary>
    /// <param name="sslEngineId">
    ///     The OpenSSL engine id.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSslEngineId(string? sslEngineId);

    /// <summary>
    ///     Enables the SSL certificate validation.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaClientsConfigurationBuilder EnableSslCertificateVerification()
    {
        WithEnableSslCertificateVerification(true);
        return this;
    }

    /// <summary>
    ///     Disables the SSL certificate validation.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaClientsConfigurationBuilder DisableSslCertificateVerification()
    {
        WithEnableSslCertificateVerification(false);
        return this;
    }

    /// <summary>
    ///     Sets the endpoint identification algorithm to be used to validate the broker hostname using the certificate. OpenSSL &gt;= 1.0.2 required.
    /// </summary>
    /// <param name="sslEndpointIdentificationAlgorithm">
    ///     The endpoint identification algorithm to be used to validate the broker hostname.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSslEndpointIdentificationAlgorithm(SslEndpointIdentificationAlgorithm? sslEndpointIdentificationAlgorithm);

    /// <summary>
    ///     Sets the Kerberos principal name that Kafka runs as, not including /hostname@REALM.
    /// </summary>
    /// <param name="saslKerberosServiceName">
    ///     The Kerberos principal name that Kafka runs as.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSaslKerberosServiceName(string? saslKerberosServiceName);

    /// <summary>
    ///     Sets the client's Kerberos principal name. (Not supported on Windows, will use the logon user's principal).
    /// </summary>
    /// <param name="saslKerberosPrincipal">
    ///     The client's Kerberos principal name. (Not supported on Windows, will use the logon user's principal).
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSaslKerberosPrincipal(string? saslKerberosPrincipal);

    /// <summary>
    ///     Sets the shell command to be used to refresh or acquire the client's Kerberos ticket. This command is executed on client creation and
    ///     every <see cref="KafkaClientConfiguration{TClientConfig}.SaslKerberosMinTimeBeforeRelogin" /> (0=disable).
    /// </summary>
    /// <param name="saslKerberosKinitCmd">
    ///     The shell command to be used to refresh or acquire the client's Kerberos ticket.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSaslKerberosKinitCmd(string? saslKerberosKinitCmd);

    /// <summary>
    ///     Sets the path to the Kerberos keytab file. This configuration property is only used as a variable in
    ///     <see cref="KafkaClientConfiguration{TClientConfig}.SaslKerberosKinitCmd" /> as <c>... -t "%{sasl.kerberos.keytab}"</c>.
    /// </summary>
    /// <param name="saslKerberosKeytab">
    ///     The path to the Kerberos keytab file.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSaslKerberosKeytab(string? saslKerberosKeytab);

    /// <summary>
    ///     Sets the minimum time in milliseconds between each key refresh attempts. Disable automatic key refresh by setting this property to 0.
    /// </summary>
    /// <param name="saslKerberosMinTimeBeforeRelogin">
    ///     The minimum time in milliseconds between each key refresh attempts.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSaslKerberosMinTimeBeforeRelogin(int? saslKerberosMinTimeBeforeRelogin);

    /// <summary>
    ///     Sets the SASL username to use with the PLAIN and SASL-SCRAM-.. mechanisms.
    /// </summary>
    /// <param name="saslUsername">
    ///     The SASL username to use with the PLAIN and SASL-SCRAM-.. mechanisms.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSaslUsername(string? saslUsername);

    /// <summary>
    ///     Sets the SASL password to use with the PLAIN and SASL-SCRAM-.. mechanisms.
    /// </summary>
    /// <param name="saslPassword">
    ///     The SASL password to use with the PLAIN and SASL-SCRAM-.. mechanisms.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSaslPassword(string? saslPassword);

    /// <summary>
    ///     Sets the SASL/OAUTHBEARER configuration. The format is implementation-dependent and must be parsed accordingly. The default unsecured token implementation
    ///     (see https://tools.ietf.org/html/rfc7515#appendix-A.5) recognizes space-separated <c>name=value</c> pairs with valid names including <c>principalClaimName</c>,
    ///     <c>principal</c>, <c>scopeClaimName</c>, <c>scope</c>, and <c>lifeSeconds</c>. The default value for <c>principalClaimName</c> is <c>"sub"</c>, the default value
    ///     for <c>scopeClaimName</c> is <c>"scope"</c>, and the default value for <c>lifeSeconds</c> is 3600. The <c>scope</c> value is CSV format with the default value being
    ///     no/empty scope. For example: <c>principalClaimName=azp principal=admin scopeClaimName=roles scope=role1,role2 lifeSeconds=600</c>. In addition, SASL extensions can be
    ///     communicated to the broker via <c>extension_NAME=value</c>. For example: <c>principal=admin extension_traceId=123</c>.
    /// </summary>
    /// <param name="saslOauthbearerConfig">
    ///     The SASL/OAUTHBEARER configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSaslOauthbearerConfig(string? saslOauthbearerConfig);

    /// <summary>
    ///     Enables the builtin unsecure JWT OAUTHBEARER token handler. This builtin handler should only be used for development or testing, and not in production.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaClientsConfigurationBuilder EnableSaslOauthbearerUnsecureJwt()
    {
        WithEnableSaslOauthbearerUnsecureJwt(true);
        return this;
    }

    /// <summary>
    ///     Disables the builtin unsecure JWT OAUTHBEARER token handler. This builtin handler should only be used for development or testing, and not in production.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaClientsConfigurationBuilder DisableSaslOauthbearerUnsecureJwt()
    {
        WithEnableSaslOauthbearerUnsecureJwt(false);
        return this;
    }

    /// <summary>
    ///     Sets the login method to be used. If set to <see cref="Confluent.Kafka.SaslOauthbearerMethod.Oidc" />, the following properties
    ///     must also be be specified: <see cref="KafkaClientConfiguration{TClientConfig}.SaslOauthbearerClientId" />,
    ///     <see cref="KafkaClientConfiguration{TClientConfig}.SaslOauthbearerClientSecret" />, and
    ///     <see cref="KafkaClientConfiguration{TClientConfig}.SaslOauthbearerTokenEndpointUrl" />.
    /// </summary>
    /// <param name="saslOauthbearerMethod">
    ///     The login method to be used.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSaslOauthbearerMethod(SaslOauthbearerMethod? saslOauthbearerMethod);

    /// <summary>
    ///     Sets the public identifier for the application. Must be unique across all clients that the authorization server handles.
    ///     Only used when <see cref="KafkaClientConfiguration{TClientConfig}.SaslOauthbearerMethod" /> is set to <see cref="Confluent.Kafka.SaslOauthbearerMethod.Oidc" />.
    /// </summary>
    /// <param name="saslOauthbearerClientId">
    ///     The public identifier for the application.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSaslOauthbearerClientId(string? saslOauthbearerClientId);

    /// <summary>
    ///     Sets the client secret only known to the application and the authorization server. This should be a sufficiently random string that is not guessable.
    ///     Only used when <see cref="KafkaClientConfiguration{TClientConfig}.SaslOauthbearerMethod" /> is set to <see cref="Confluent.Kafka.SaslOauthbearerMethod.Oidc" />.
    /// </summary>
    /// <param name="saslOauthbearerClientSecret">
    ///     The client secret only known to the application and the authorization server.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSaslOauthbearerClientSecret(string? saslOauthbearerClientSecret);

    /// <summary>
    ///     Sets the scope of the access request to the broker.
    ///     Only used when <see cref="KafkaClientConfiguration{TClientConfig}.SaslOauthbearerMethod" /> is set to <see cref="Confluent.Kafka.SaslOauthbearerMethod.Oidc" />.
    /// </summary>
    /// <param name="saslOauthbearerScope">
    ///     The scope of the access request to the broker.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSaslOauthbearerScope(string? saslOauthbearerScope);

    /// <summary>
    ///     Sets the additional information to be provided to the broker as a comma-separated list of <c>key=value</c> pairs (e.g. <c>supportFeatureX=true,organizationId=sales-emea</c>).
    ///     Only used when <see cref="KafkaClientConfiguration{TClientConfig}.SaslOauthbearerMethod" /> is set to <see cref="Confluent.Kafka.SaslOauthbearerMethod.Oidc" />.
    /// </summary>
    /// <param name="saslOauthbearerExtensions">
    ///     The additional information to be provided to the broker as a comma-separated list of <c>key=value</c> pairs.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithSaslOauthbearerExtensions(string? saslOauthbearerExtensions);

    /// <summary>
    ///     Sets the OAuth/OIDC issuer token endpoint HTTP(S) URI used to retrieve the token.
    ///     Only used when <see cref="KafkaClientConfiguration{TClientConfig}.SaslOauthbearerMethod" /> is set to <see cref="Confluent.Kafka.SaslOauthbearerMethod.Oidc" />.
    /// </summary>
    /// <param name="saslOauthbearerTokenEndpointUrl">
    ///     The OAuth/OIDC issuer token endpoint HTTP(S) URI used to retrieve the token.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "Declared as string in the underlying library")]
    public partial KafkaClientsConfigurationBuilder WithSaslOauthbearerTokenEndpointUrl(string? saslOauthbearerTokenEndpointUrl);

    /// <summary>
    ///     Sets the list of plugin libraries to load (<c>;</c> separated). The library search path is platform dependent. If no filename extension
    ///     is specified the platform-specific extension (such as .dll or .so) will be appended automatically.
    /// </summary>
    /// <param name="pluginLibraryPaths">
    ///     The list of plugin libraries to load (<c>;</c> separated).
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithPluginLibraryPaths(string? pluginLibraryPaths);

    /// <summary>
    ///     Sets the rack identifier for this client. This can be any string value which indicates where this client is physically located.
    /// </summary>
    /// <param name="clientRack">
    ///     The rack identifier for this client.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithClientRack(string? clientRack);

    /// <summary>
    ///     Sets the maximum time (in milliseconds) before a cancellation request is acted on. Low values may result in measurably higher CPU usage.
    /// </summary>
    /// <param name="cancellationDelayMaxMs">
    ///     The maximum time (in milliseconds) before a cancellation request is acted on.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaClientsConfigurationBuilder WithCancellationDelayMaxMs(int cancellationDelayMaxMs);

    internal KafkaClientsConfigurationActions GetConfigurationActions()
    {
        foreach (Action<IKafkaClientConfigurationBuilder> sharedAction in _sharedConfigurationActions)
        {
            _configurationActions.ProducerConfigurationActions.PrependToAll(sharedAction.Invoke);
            _configurationActions.ConsumerConfigurationActions.PrependToAll(sharedAction.Invoke);
        }

        return _configurationActions;
    }
}
