// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Confluent.Kafka;
using Confluent.SchemaRegistry;

namespace Silverback.Tools.Generators.KafkaConfigProxies;

[SuppressMessage("ReSharper", "CognitiveComplexity", Justification = "Don't care")]
[SuppressMessage("Maintainability", "CA1502: Avoid excessive complexity", Justification = "Don't care")]
public static class DocumentationProvider
{
    public static void AppendSummary(this StringBuilder builder, PropertyInfo property)
    {
        builder.AppendLine("    /// <summary>");

        switch (property.DeclaringType!.Name)
        {
            case nameof(Config):
                AppendSummaryForConfig(builder, property);
                break;
            case nameof(ClientConfig):
                AppendSummaryForClientConfig(builder, property);
                break;
            case nameof(ConsumerConfig):
                AppendSummaryForConsumerConfig(builder, property);
                break;
            case nameof(ProducerConfig):
                AppendSummaryForProducerConfig(builder, property);
                break;
            case nameof(SchemaRegistryConfig):
                AppendSummaryForSchemaRegistryConfig(builder, property);
                break;
        }

        builder.AppendLine("    /// </summary>");
    }

    private static void AppendSummaryForConfig(StringBuilder builder, PropertyInfo property)
    {
        switch (property.Name)
        {
            case nameof(Config.CancellationDelayMaxMs):
                builder.AppendLine("    ///     Gets the maximum time (in milliseconds) before a cancellation request is acted on. Low values may result in measurably higher CPU usage.");
                break;
        }
    }

    private static void AppendSummaryForClientConfig(StringBuilder builder, PropertyInfo property)
    {
        switch (property.Name)
        {
            case nameof(ClientConfig.SaslMechanism):
                builder.AppendLine("    ///     Gets the SASL mechanism to use for authentication. Supported: GSSAPI, PLAIN, SCRAM-SHA-256, SCRAM-SHA-512.");
                break;
            case nameof(ClientConfig.Acks):
                builder.AppendLine("    ///     Gets the number of acknowledgements that the leader broker must receive from the in-sync replicas before responding to the request:");
                builder.AppendLine("    ///     <see cref=\"Confluent.Kafka.Acks.None\"/> (no response/ack is sent to the client), <see cref=\"Confluent.Kafka.Acks.Leader\"/> (the leader will write the record to");
                builder.AppendLine("    ///     its local log but will respond without awaiting full acknowledgement from all followers, or <see cref=\"Confluent.Kafka.Acks.All\"/> (the broker");
                builder.AppendLine("    ///     will block until the message is committed by all in-sync replicas. If there are less than <c>min.insync.replicas</c> (broker configuration)");
                builder.AppendLine("    ///     in the in-sync replicas set the produce request will fail.");
                break;
            case nameof(ClientConfig.ClientId):
                builder.AppendLine("    ///     Gets the client identifier.");
                break;
            case nameof(ClientConfig.BootstrapServers):
                builder.AppendLine("    ///     Gets the comma-separated list of brokers (host or host:port).");
                break;
            case nameof(ClientConfig.MessageMaxBytes):
                builder.AppendLine("    ///     Gets the maximum message size.");
                break;
            case nameof(ClientConfig.MessageCopyMaxBytes):
                builder.AppendLine("    ///     Gets the maximum size for a message to be copied into the buffer. Messages larger than this will be passed by reference (zero-copy)");
                builder.AppendLine("    ///     at the expense of larger iovecs.");
                break;
            case nameof(ClientConfig.ReceiveMessageMaxBytes):
                builder.AppendLine("    ///     Gets the maximum response message size. This serves as a safety precaution to avoid memory exhaustion in case of protocol hickups.");
                builder.AppendLine("    ///     This value must be at least <see cref=\"KafkaConsumerConfiguration.FetchMaxBytes\"/> + 512 to allow for protocol overhead.");
                builder.AppendLine("    ///     The value is adjusted automatically unless the configuration property is explicitly set.");
                break;
            case nameof(ClientConfig.MaxInFlight):
                builder.AppendLine("    ///     Gets the maximum number of in-flight requests per broker connection. This is a generic property applied to all broker communication,");
                builder.AppendLine("    ///     however it is primarily relevant to produce requests. In particular, note that other mechanisms limit the number of outstanding consumer");
                builder.AppendLine("    ///     fetch request per broker to one.");
                break;
            case nameof(ClientConfig.TopicMetadataRefreshIntervalMs):
                builder.AppendLine("    ///     Gets the interval (in milliseconds) at which the topic and broker metadata is refreshed in order to proactively discover any new");
                builder.AppendLine("    ///     brokers, topics, partitions or partition leader changes. Use -1 to disable the intervalled refresh (not recommended). If there are");
                builder.AppendLine("    ///     no locally referenced topics (no topic objects created, no messages produced, no subscription or no assignment) then only the broker");
                builder.AppendLine("    ///     list will be refreshed every interval but no more often than every 10s.");
                break;
            case nameof(ClientConfig.MetadataMaxAgeMs):
                builder.AppendLine("    ///     Gets the metadata cache max age (in milliseconds). Defaults to <see cref=\"TopicMetadataRefreshIntervalMs\" />.");
                break;
            case nameof(ClientConfig.TopicMetadataRefreshFastIntervalMs):
                builder.AppendLine("    ///     Gets the refresh interval (in milliseconds) to be applied instead of the <see cref=\"TopicMetadataRefreshIntervalMs\" />");
                builder.AppendLine("    ///     when a topic loses its leader and a new metadata request will be enqueued. This initial interval will be exponentially increased");
                builder.AppendLine("    ///     until the topic metadata has been refreshed. This is used to recover quickly from transitioning leader brokers.");
                break;
            case nameof(ClientConfig.TopicMetadataRefreshSparse):
                builder.AppendLine("    ///     Gets a value indicating whether less metadata requests must be performed (consuming less network bandwidth).");
                break;
            case nameof(ClientConfig.TopicMetadataPropagationMaxMs):
                builder.AppendLine("    ///     Gets the delay (in milliseconds) to be applied before marking a topic as non-existent. The maximum propagation time is calculated");
                builder.AppendLine("    ///     from the time the topic is first referenced in the client.");
                break;
            case nameof(ClientConfig.TopicBlacklist):
                builder.AppendLine("///     Gets a comma-separated list of regular expressions for matching topic names that should be ignored in broker metadata information");
                builder.AppendLine("///     as if the topics did not exist.");
                break;
            case nameof(ClientConfig.Debug):
                builder.AppendLine("    ///     Gets a comma-separated list of debug contexts to enable.");
                builder.AppendLine("    ///     Detailed producer debugging: <c>broker,topic,msg</c>.");
                builder.AppendLine("    ///     Detailed consumer debugging: <c>consumer,cgrp,topic,fetch</c>.");
                break;
            case nameof(ClientConfig.SocketTimeoutMs):
                builder.AppendLine("    ///     Gets the default timeout (in milliseconds) for network requests.");
                break;
            case nameof(ClientConfig.SocketSendBufferBytes):
                builder.AppendLine("    ///     Gets the socket send buffer size. The system default is used if 0.");
                break;
            case nameof(ClientConfig.SocketReceiveBufferBytes):
                builder.AppendLine("    ///     Gets the socket receive buffer size. The system default is used if 0.");
                break;
            case nameof(ClientConfig.SocketKeepaliveEnable):
                builder.AppendLine("    ///     Gets a value indicating whether TCP keep-alive (SO_KEEPALIVE) must be enabled on the broker sockets.");
                break;
            case nameof(ClientConfig.SocketNagleDisable):
                builder.AppendLine("    ///     Gets a value indicating whether the Nagle's algorithm (TCP_NODELAY) must be disabled on broker sockets.");
                break;
            case nameof(ClientConfig.SocketMaxFails):
                builder.AppendLine("    ///     Gets the maximum number of send failures (e.g. timed out requests) before disconnecting. Disable with 0.<br />");
                builder.AppendLine("    ///     Warning: It is highly recommended to leave this setting at its default value of 1 to avoid the client and broker to");
                builder.AppendLine("    ///     become desynchronized in case of request timeouts.<br />");
                builder.AppendLine("    ///     Note: The connection is automatically re-established.");
                break;
            case nameof(ClientConfig.BrokerAddressTtl):
                builder.AppendLine("    ///     Gets the duration in milliseconds of the cache of the broker address resolving results. ");
                break;
            case nameof(ClientConfig.BrokerAddressFamily):
                builder.AppendLine("    ///     Gets the allowed broker IP address families.");
                break;
            case nameof(ClientConfig.SocketConnectionSetupTimeoutMs):
                builder.AppendLine("    ///     Gets the maximum time (in milliseconds) allowed for the setup of the broker connection (TCP connection setup and SSL/SASL handshake).");
                builder.AppendLine("    ///     The connection to the broker will be closed and retried, if the timeout elapses before it is fully functional.");
                break;
            case nameof(ClientConfig.ConnectionsMaxIdleMs):
                builder.AppendLine("    ///     Gets the maximum time of inactivity (in milliseconds) before closing the broker connections. Disable with 0.");
                builder.AppendLine("    ///     If this property is left at its default value some heuristics are performed to determine a suitable default value.");
                break;
            case nameof(ClientConfig.ReconnectBackoffMs):
                builder.AppendLine("    ///     Gets the initial time (in milliseconds) to wait before reconnecting to a broker after the connection has been closed. The time is increased");
                builder.AppendLine("    ///     exponentially until <see cref=\"ReconnectBackoffMaxMs\" /> is reached. -25% to +50% jitter is applied to each reconnect backoff.");
                builder.AppendLine("    ///     A value of 0 disables the backoff and reconnects immediately.");
                break;
            case nameof(ClientConfig.ReconnectBackoffMaxMs):
                builder.AppendLine("    ///     Gets the maximum time (in milliseconds) to wait before reconnecting to a broker after the connection has been closed.");
                break;
            case nameof(ClientConfig.StatisticsIntervalMs):
                builder.AppendLine("    ///     Gets the statistics emit interval (in milliseconds). The granularity is 1000ms. A value of 0 disables statistics.");
                break;
            case nameof(ClientConfig.ApiVersionRequest):
                builder.AppendLine("    ///     Gets a value indicating whether the broker's supported API versions must be requested to adjust the functionality to the available protocol features.");
                builder.AppendLine("    ///     If set to <c>false</c>, or the API version request fails, the fallback version <see cref=\"BrokerVersionFallback\" /> will be used.");
                break;
            case nameof(ClientConfig.ApiVersionRequestTimeoutMs):
                builder.AppendLine("    ///     Gets the timeout (in milliseconds) for the broker API version requests.");
                break;
            case nameof(ClientConfig.ApiVersionFallbackMs):
                builder.AppendLine("    ///     Gets how long the <see cref=\"BrokerVersionFallback\" /> is used in the case the API version request fails.");
                break;
            case nameof(ClientConfig.BrokerVersionFallback):
                builder.AppendLine("    ///     Gets the broker API version to be used when the API version request fails or it's disabled. Older broker versions (before 0.10.0) don't support");
                builder.AppendLine("    ///     the API version request. Valid values are: 0.9.0, 0.8.2, 0.8.1, 0.8.0. Any other value &gt;= 0.10, such as 0.10.2.1, enables the <see cref=\"ApiVersionRequest\" />.");
                break;
            case nameof(ClientConfig.AllowAutoCreateTopics):
                builder.AppendLine("    ///     Gets a value indicating whether topics can be automatically created on the broker when subscribing to or assigning a non-existent");
                builder.AppendLine("    ///     topic. The broker must also be configured with <c>auto.create.topics.enable=true</c> for this configuration to take effect.");
                builder.AppendLine("    ///     Requires broker version &gt;= 0.11.0.0, for older broker versions only the broker configuration applies.");
                break;
            case nameof(ClientConfig.SecurityProtocol):
                builder.AppendLine("    ///     Gets the protocol to be used to communicate with the brokers.");
                break;
            case nameof(ClientConfig.SslCipherSuites):
                builder.AppendLine("    ///     Gets the SSL cipher suites.");
                break;
            case nameof(ClientConfig.SslCurvesList):
                builder.AppendLine("    ///     Gets the supported SSL curves.");
                break;
            case nameof(ClientConfig.SslSigalgsList):
                builder.AppendLine("    ///     Gets the supported SSL signature algorithms.");
                break;
            case nameof(ClientConfig.SslKeyLocation):
                builder.AppendLine("    ///     Gets the path to the client's private key (PEM) used for the authentication.");
                break;
            case nameof(ClientConfig.SslKeyPassword):
                builder.AppendLine("    ///     Gets the private key passphrase.");
                break;
            case nameof(ClientConfig.SslKeyPem):
                builder.AppendLine("    ///     Gets the client's private key string (in PEM format) used for the authentication.");
                break;
            case nameof(ClientConfig.SslCertificateLocation):
                builder.AppendLine("    ///     Gets the path to the client's public key (PEM) used for the authentication.");
                break;
            case nameof(ClientConfig.SslCertificatePem):
                builder.AppendLine("    ///     Gets the client's public key string (in PEM format) used for the authentication.");
                break;
            case nameof(ClientConfig.SslCaLocation):
                builder.AppendLine("    ///     Gets the file or directory path to the CA certificate(s) for verifying the broker's key. Defaults: On Windows the system's CA certificates are automatically looked up in the Windows Root certificate store.");
                builder.AppendLine("    ///     On Mac OSX this configuration defaults to <c>probe</c>. It is recommended to install openssl using Homebrew, to provide CA certificates. On Linux install the distribution's ca-certificates package.");
                builder.AppendLine("    ///     If OpenSSL is statically linked or <see cref=\"SslCaLocation\" /> is set to <c>probe</c> a list of standard paths will be probed and the first one found will be used as the default CA certificate location path.");
                builder.AppendLine("    ///     If OpenSSL is dynamically linked the OpenSSL library's default path will be used (see <c>OPENSSLDIR</c> in <c>openssl version -a</c>).");
                break;
            case nameof(ClientConfig.SslCaPem):
                builder.AppendLine("    ///     Gets the CA certificate string (in PEM format) for verifying the broker's key.");
                break;
            case nameof(ClientConfig.SslCaCertificateStores):
                builder.AppendLine("    ///     Gets a comma-separated list of Windows certificate stores to load CA certificates from. The certificates will be loaded in the same order");
                builder.AppendLine("    ///     as stores are specified. If no certificates can be loaded from any of the specified stores an error is logged and the OpenSSL library's default");
                builder.AppendLine("    ///     CA location is used instead. Store names are typically one or more of: MY, Root, Trust, CA.");
                break;
            case nameof(ClientConfig.SslCrlLocation):
                builder.AppendLine("    ///     Gets the path to the certificate revocation list (CRL) for verifying broker's certificate validity.");
                break;
            case nameof(ClientConfig.SslKeystoreLocation):
                builder.AppendLine("    ///     Gets the path to the client's keystore (PKCS#12) used for the authentication.");
                break;
            case nameof(ClientConfig.SslKeystorePassword):
                builder.AppendLine("    ///     Gets the client's keystore (PKCS#12) password.");
                break;
            case nameof(ClientConfig.SslProviders):
                builder.AppendLine("    ///     Gets the comma-separated list of OpenSSL 3.0.x implementation providers.");
                break;
            case nameof(ClientConfig.SslEngineLocation):
                builder.AppendLine("    ///     Gets the path to the OpenSSL engine library. OpenSSL &gt;= 1.1.0 required.");
                break;
            case nameof(ClientConfig.SslEngineId):
                builder.AppendLine("    ///     Gets the OpenSSL engine id (the name used for loading engine).");
                break;
            case nameof(ClientConfig.EnableSslCertificateVerification):
                builder.AppendLine("    ///     Gets a value indicating whether the broker (server) certificate must be verified.");
                break;
            case nameof(ClientConfig.SslEndpointIdentificationAlgorithm):
                builder.AppendLine("    ///     Gets the endpoint identification algorithm to be used to validate the broker hostname using the certificate. OpenSSL &gt;= 1.0.2 required.");
                break;
            case nameof(ClientConfig.SaslKerberosServiceName):
                builder.AppendLine("    ///     Gets the Kerberos principal name that Kafka runs as, not including /hostname@REALM.");
                break;
            case nameof(ClientConfig.SaslKerberosPrincipal):
                builder.AppendLine("    ///     Gets the client's Kerberos principal name. (Not supported on Windows, will use the logon user's principal).");
                break;
            case nameof(ClientConfig.SaslKerberosKinitCmd):
                builder.AppendLine("    ///     Gets the shell command to be used to refresh or acquire the client's Kerberos ticket. This command is executed on client creation and every <see cref=\"SaslKerberosMinTimeBeforeRelogin\" /> (0=disable).");
                break;
            case nameof(ClientConfig.SaslKerberosKeytab):
                builder.AppendLine("    ///     Gets the path to the Kerberos keytab file. This configuration property is only used as a variable in <see cref=\"SaslKerberosKinitCmd\" /> as <c>... -t \"%{sasl.kerberos.keytab}\"</c>.");
                break;
            case nameof(ClientConfig.SaslKerberosMinTimeBeforeRelogin):
                builder.AppendLine("    ///     Gets the minimum time in milliseconds between each key refresh attempts. Disable automatic key refresh by setting this property to 0.");
                break;
            case nameof(ClientConfig.SaslUsername):
                builder.AppendLine("    ///     Gets the SASL username to use with the PLAIN and SASL-SCRAM-.. mechanisms.");
                break;
            case nameof(ClientConfig.SaslPassword):
                builder.AppendLine("    ///     Gets the SASL password to use with the PLAIN and SASL-SCRAM-.. mechanisms.");
                break;
            case nameof(ClientConfig.SaslOauthbearerConfig):
                builder.AppendLine("    ///     Gets the SASL/OAUTHBEARER configuration. The format is implementation-dependent and must be parsed accordingly. The default unsecured token implementation");
                builder.AppendLine("    ///     (see https://tools.ietf.org/html/rfc7515#appendix-A.5) recognizes space-separated <c>name=value</c> pairs with valid names including <c>principalClaimName</c>,");
                builder.AppendLine("    ///     <c>principal</c>, <c>scopeClaimName</c>, <c>scope</c>, and <c>lifeSeconds</c>. The default value for <c>principalClaimName</c> is <c>\"sub\"</c>, the default value");
                builder.AppendLine("    ///     for <c>scopeClaimName</c> is <c>\"scope\"</c>, and the default value for <c>lifeSeconds</c> is 3600. The <c>scope</c> value is CSV format with the default value being");
                builder.AppendLine("    ///     no/empty scope. For example: <c>principalClaimName=azp principal=admin scopeClaimName=roles scope=role1,role2 lifeSeconds=600</c>. In addition, SASL extensions can be");
                builder.AppendLine("    ///     communicated to the broker via <c>extension_NAME=value</c>. For example: <c>principal=admin extension_traceId=123</c>.");
                break;
            case nameof(ClientConfig.EnableSaslOauthbearerUnsecureJwt):
                builder.AppendLine("    ///     Gets a value indicating whether the builtin unsecure JWT OAUTHBEARER token handler must be enabled. This builtin handler should only be used for development or testing, and not in production.");
                break;
            case nameof(ClientConfig.SaslOauthbearerMethod):
                builder.AppendLine("    ///     Gets the login method to be used. If set to <see cref=\"Confluent.Kafka.SaslOauthbearerMethod.Oidc\" />, the following properties must also be be specified: <see cref=\"SaslOauthbearerClientId\" />,");
                builder.AppendLine("    ///     <see cref=\"SaslOauthbearerClientSecret\" />, and <see cref=\"SaslOauthbearerTokenEndpointUrl\" />.");
                break;
            case nameof(ClientConfig.SaslOauthbearerClientId):
                builder.AppendLine("    ///     Gets the public identifier for the application. Must be unique across all clients that the authorization server handles. Only used when <see cref=\"SaslOauthbearerMethod\" /> is set to <see cref=\"Confluent.Kafka.SaslOauthbearerMethod.Oidc\" />.");
                break;
            case nameof(ClientConfig.SaslOauthbearerClientSecret):
                builder.AppendLine("    ///     Gets the client secret only known to the application and the authorization server. This should be a sufficiently random string that is not guessable. Only used when <see cref=\"SaslOauthbearerMethod\" /> is set to <see cref=\"Confluent.Kafka.SaslOauthbearerMethod.Oidc\" />.");
                break;
            case nameof(ClientConfig.SaslOauthbearerScope):
                builder.AppendLine("    ///     Gets the scope of the access request to the broker. Only used when <see cref=\"SaslOauthbearerMethod\" /> is set to <see cref=\"Confluent.Kafka.SaslOauthbearerMethod.Oidc\" />.");
                break;
            case nameof(ClientConfig.SaslOauthbearerExtensions):
                builder.AppendLine("    ///     Gets the additional information to be provided to the broker as a comma-separated list of <c>key=value</c> pairs (e.g. <c>supportFeatureX=true,organizationId=sales-emea</c>). Only used when <see cref=\"SaslOauthbearerMethod\" /> is set to <see cref=\"Confluent.Kafka.SaslOauthbearerMethod.Oidc\" />.");
                break;
            case nameof(ClientConfig.SaslOauthbearerTokenEndpointUrl):
                builder.AppendLine("    ///     Gets the OAuth/OIDC issuer token endpoint HTTP(S) URI used to retrieve the token. Only used when <see cref=\"SaslOauthbearerMethod\" /> is set to <see cref=\"Confluent.Kafka.SaslOauthbearerMethod.Oidc\" />.");
                break;
            case nameof(ClientConfig.PluginLibraryPaths):
                builder.AppendLine("    ///     Gets the list of plugin libraries to load (<c>;</c> separated). The library search path is platform dependent. If no filename extension is specified the platform-specific extension (such as .dll or .so) will be appended automatically.");
                break;
            case nameof(ClientConfig.ClientRack):
                builder.AppendLine("    ///     Gets the rack identifier for this client. This can be any string value which indicates where this client is physically located.");
                break;
        }
    }

    private static void AppendSummaryForConsumerConfig(StringBuilder builder, PropertyInfo property)
    {
        switch (property.Name)
        {
            case nameof(ConsumerConfig.ConsumeResultFields):
                builder.AppendLine("    ///     Gets a comma-separated list of fields that may be optionally set in <see cref=\"ConsumeResult{TKey,TValue}\" /> objects returned by");
                builder.AppendLine("    ///     the <see cref=\"Consumer{TKey,TValue}.Consume(System.TimeSpan)\" /> method. Disabling fields that you do not require will improve");
                builder.AppendLine("    ///     throughput and reduce memory consumption. Allowed values: <c>headers</c>, <c>timestamp</c>, <c>topic</c>, <c>all</c>, <c>none</c>.");
                break;
            case nameof(ConsumerConfig.AutoOffsetReset):
                builder.AppendLine("    ///     Gets the action to take when there is no initial offset in the offset store or the desired offset is out of range:");
                builder.AppendLine("    ///     <see cref=\"Confluent.Kafka.AutoOffsetReset.Earliest\" /> to automatically reset to the smallest offset,");
                builder.AppendLine("    ///     <see cref=\"Confluent.Kafka.AutoOffsetReset.Latest\" /> to automatically reset to the largest offset, and");
                builder.AppendLine("    ///     <see cref=\"Confluent.Kafka.AutoOffsetReset.Error\" /> to trigger an error (ERR__AUTO_OFFSET_RESET).");
                break;
            case nameof(ConsumerConfig.GroupInstanceId):
                builder.AppendLine("    ///     Gets the static instance id used to enable static group membership. Static group members are able to leave and rejoin a group within");
                builder.AppendLine("    ///     the configured <see cref=\"SessionTimeoutMs\" /> without prompting a group rebalance. This should be used in combination with a larger");
                builder.AppendLine("    ///     <see cref=\"SessionTimeoutMs\" /> to avoid group rebalances caused by transient unavailability (e.g. process restarts).");
                builder.AppendLine("    ///     Requires broker version &gt;= 2.3.0.");
                break;
            case nameof(ConsumerConfig.PartitionAssignmentStrategy):
                builder.AppendLine("    ///     Gets the partition assignment strategy: <see cref=\"Confluent.Kafka.PartitionAssignmentStrategy.Range\" /> to co-localize the partitions");
                builder.AppendLine("    ///     of several topics, <see cref=\"Confluent.Kafka.PartitionAssignmentStrategy.RoundRobin\" /> to evenly distribute the partitions among");
                builder.AppendLine("    ///     the consumer group members, <see cref=\"Confluent.Kafka.PartitionAssignmentStrategy.CooperativeSticky\" /> to evenly distribute the");
                builder.AppendLine("    ///     partitions and limit minimize the partitions movements. The default is <see cref=\"Confluent.Kafka.PartitionAssignmentStrategy.Range\" />.");
                break;
            case nameof(ConsumerConfig.SessionTimeoutMs):
                builder.AppendLine("    ///     Gets the client group session and failure detection timeout (in milliseconds). The consumer sends periodic heartbeats");
                builder.AppendLine("    ///     <see cref=\"HeartbeatIntervalMs\" /> to indicate its liveness to the broker. If no heartbeat is received by the broker for a group");
                builder.AppendLine("    ///     member within the session timeout, the broker will remove the consumer from the group and trigger a rebalance. Also see");
                builder.AppendLine("    ///     <see cref=\"MaxPollIntervalMs\" />.");
                break;
            case nameof(ConsumerConfig.HeartbeatIntervalMs):
                builder.AppendLine("    ///     Gets the interval (in milliseconds) at which the heartbeats have to be sent to the broker.");
                break;
            case nameof(ConsumerConfig.GroupProtocolType):
                builder.AppendLine("    ///     Gets the group protocol type.");
                break;
            case nameof(ConsumerConfig.CoordinatorQueryIntervalMs):
                builder.AppendLine("    ///     Gets the interval (in milliseconds) at which the current group coordinator must be queried. If the currently assigned coordinator");
                builder.AppendLine("    ///     is down the configured query interval will be divided by ten to more quickly recover in case of coordinator reassignment.");
                break;
            case nameof(ConsumerConfig.MaxPollIntervalMs):
                builder.AppendLine("    ///     Gets the maximum allowed time (in milliseconds) between calls to consume messages. If this interval is exceeded the consumer is");
                builder.AppendLine("    ///     considered failed and the group will rebalance in order to reassign the partitions to another consumer group member.<br />");
                builder.AppendLine("    ///     Warning: Offset commits may be not possible at this point.");
                break;
            case nameof(ConsumerConfig.AutoCommitIntervalMs):
                builder.AppendLine("    ///     Gets the frequency in milliseconds at which the consumer offsets are committed.");
                break;
            case nameof(ConsumerConfig.QueuedMinMessages):
                builder.AppendLine("    ///     Gets the minimum number of messages per topic and partition that the underlying library must try to maintain in the local consumer queue.");
                break;
            case nameof(ConsumerConfig.QueuedMaxMessagesKbytes):
                builder.AppendLine("    ///     Gets the maximum number of kilobytes of queued pre-fetched messages to store in the local consumer queue. This setting applies to");
                builder.AppendLine("    ///     the single consumer queue, regardless of the number of partitions. This value may be overshot by <see cref=\"FetchMaxBytes\" />. This");
                builder.AppendLine("    ///     property has higher priority than <see cref=\"QueuedMinMessages\" />.");
                break;
            case nameof(ConsumerConfig.FetchWaitMaxMs):
                builder.AppendLine("    ///     Gets the maximum time (in milliseconds) that the broker may wait to fill the fetch response with enough messages to match the");
                builder.AppendLine("    ///     size specified by <see cref=\"FetchMinBytes\" />.");
                break;
            case nameof(ConsumerConfig.MaxPartitionFetchBytes):
                builder.AppendLine("    ///     Gets the initial maximum number of bytes per topic and partition to request when fetching messages from the broker. If the client");
                builder.AppendLine("    ///     encounters a message larger than this value it will gradually try to increase it until the entire message can be fetched.");
                break;
            case nameof(ConsumerConfig.FetchMaxBytes):
                builder.AppendLine("    ///     Gets the maximum amount of data the broker shall return for a fetch request. The messages are fetched in batches by the consumer");
                builder.AppendLine("    ///     and if the first message batch in the first non-empty partition of the fetch request is larger than this value, then the message");
                builder.AppendLine("    ///     batch will still be returned to ensure that the consumer can make progress. The maximum message batch size accepted by the broker");
                builder.AppendLine("    ///     is defined via <c>message.max.bytes</c> (broker config) or <c>max.message.bytes</c> (broker topic config). This value is automatically");
                builder.AppendLine("    ///     adjusted upwards to be at least <c>message.max.bytes</c> (consumer config).");
                break;
            case nameof(ConsumerConfig.FetchMinBytes):
                builder.AppendLine("    ///     Gets the minimum number of bytes that the broker must respond with. If <see cref=\"FetchWaitMaxMs\" /> expires the accumulated data");
                builder.AppendLine("    ///     will be sent to the client regardless of this setting.");
                break;
            case nameof(ConsumerConfig.FetchErrorBackoffMs):
                builder.AppendLine("    ///     Gets how long to postpone the next fetch request for a topic and partition in case of a fetch error.");
                break;
            case nameof(ConsumerConfig.IsolationLevel):
                builder.AppendLine("    ///     Gets a value indicating how to read the messages written inside a transaction: <see cref=\"Confluent.Kafka.IsolationLevel.ReadCommitted\" />");
                builder.AppendLine("    ///     to only return transactional messages which have been committed, or <see cref=\"Confluent.Kafka.IsolationLevel.ReadUncommitted\" /> to");
                builder.AppendLine("    ///     return all messages, even transactional messages which have been aborted.");
                break;
            case nameof(ConsumerConfig.EnablePartitionEof):
                builder.AppendLine("    ///     Gets a value indicating whether the partition EOF event must be emitted whenever the consumer reaches the end of a partition.");
                break;
            case nameof(ConsumerConfig.CheckCrcs):
                builder.AppendLine("    ///     Gets a value indicating whether the CRC32 of the consumed messages must be verified, ensuring no on-the-wire or on-disk corruption");
                builder.AppendLine("    ///     to the messages occurred. This check comes at slightly increased CPU usage.");
                break;
        }
    }

    private static void AppendSummaryForProducerConfig(StringBuilder builder, PropertyInfo property)
    {
        switch (property.Name)
        {
            case nameof(ProducerConfig.EnableDeliveryReports):
                builder.AppendLine("    ///     Gets a value indicating whether delivery reports must be sent. Typically you should set this parameter to <c>true</c>. Set it to");
                builder.AppendLine("    ///     <c>false</c> for \"fire and forget\" semantics and a small boost in performance.");
                break;
            case nameof(ProducerConfig.RequestTimeoutMs):
                builder.AppendLine("    ///     Gets the ack timeout of the producer request in milliseconds. This value is only enforced by the broker and relies on");
                builder.AppendLine("    ///     <c>request.required.acks</c> being != 0.");
                break;
            case nameof(ProducerConfig.MessageTimeoutMs):
                builder.AppendLine("    ///     Gets the local message timeout (in milliseconds). This value is only enforced locally and limits the time a produced message waits");
                builder.AppendLine("    ///     for successful delivery. A time of 0 is infinite. This is the maximum time to deliver a message (including retries) and a delivery");
                builder.AppendLine("    ///     error will occur when either the retry count or the message timeout are exceeded. The message timeout is automatically adjusted to");
                builder.AppendLine("    ///     <see cref=\"TransactionTimeoutMs\" /> if  <see cref=\"TransactionalId\"/> is set.");
                break;
            case nameof(ProducerConfig.Partitioner):
                builder.AppendLine("    ///     Gets the partitioner to be used to decide the target partition for a message: <see cref=\"Confluent.Kafka.Partitioner.Random\" />");
                builder.AppendLine("    ///     to randomly distribute the messages, <see cref=\"Confluent.Kafka.Partitioner.Consistent\" /> to use the CRC32 hash of the message key");
                builder.AppendLine("    ///     (empty and null keys are mapped to a single partition), <see cref=\"Confluent.Kafka.Partitioner.ConsistentRandom\" /> to use the CRC32");
                builder.AppendLine("    ///     hash of the message key (but empty and null keys are randomly partitioned), <see cref=\"Confluent.Kafka.Partitioner.Murmur2\" /> to use");
                builder.AppendLine("    ///     a Java Producer compatible Murmur2 hash of the message key (null keys are mapped to a single partition), or");
                builder.AppendLine("    ///     <see cref=\"Confluent.Kafka.Partitioner.Murmur2Random\" /> to use a Java Producer compatible Murmur2 hash of the message key (but null");
                builder.AppendLine("    ///     keys are randomly partitioned).<br />");
                builder.AppendLine("    ///     The default is <see cref=\"Confluent.Kafka.Partitioner.ConsistentRandom\" />, while <see cref=\"Confluent.Kafka.Partitioner.Murmur2Random\" />");
                builder.AppendLine("    ///     is functionally equivalent to the default partitioner in the Java Producer.");
                break;
            case nameof(ProducerConfig.CompressionLevel):
                builder.AppendLine("    ///     Gets the compression level parameter for the algorithm selected by configuration property <see cref=\"CompressionType\" />. Higher");
                builder.AppendLine("    ///     values will result in better compression at the cost of higher CPU usage. Usable range is algorithm-dependent: [0-9] for gzip,");
                builder.AppendLine("    ///     [0-12] for lz4, only 0 for snappy. -1 = codec-dependent default compression level.");
                break;
            case nameof(ProducerConfig.TransactionalId):
                builder.AppendLine("    ///     Gets the identifier to be used to identify the same transactional producer instance across process restarts. This is required to");
                builder.AppendLine("    ///     enable the transactional producer and it allows the producer to guarantee that transactions corresponding to earlier instances of");
                builder.AppendLine("    ///     the same producer have been finalized prior to starting any new transaction, and that any zombie instances are fenced off. If no");
                builder.AppendLine("    ///     <see cref=\"TransactionalId\" /> is provided, then the producer is limited to idempotent delivery (see <see cref=\"EnableIdempotence\" />).");
                builder.AppendLine("    ///     Requires broker version &gt;= 0.11.0.");
                break;
            case nameof(ProducerConfig.TransactionTimeoutMs):
                builder.AppendLine("    ///     Gets the maximum amount of time in milliseconds that the transaction coordinator will wait for a transaction status update from");
                builder.AppendLine("    ///     the producer before proactively aborting the ongoing transaction. If this value is larger than the <c>transaction.max.timeout.ms</c>");
                builder.AppendLine("    ///     setting in the broker, the init transaction call will fail with ERR_INVALID_TRANSACTION_TIMEOUT. The transaction timeout automatically");
                builder.AppendLine("    ///     adjusts <see cref=\"MessageTimeoutMs\" /> and <see cref=\"KafkaClientConfiguration{TClientConfig}.SocketTimeoutMs\" /> unless explicitly configured in which case");
                builder.AppendLine("    ///     they must not exceed the transaction timeout (<see cref=\"KafkaClientConfiguration{TClientConfig}.SocketTimeoutMs\" /> must be at least 100ms lower than");
                builder.AppendLine("    ///     <see cref=\"TransactionTimeoutMs\" />).");
                break;
            case nameof(ProducerConfig.EnableIdempotence):
                builder.AppendLine("    ///     Gets a value indicating whether the producer must ensure that messages are successfully produced exactly once and in the original");
                builder.AppendLine("    ///     produce order. The following configuration properties are adjusted automatically (if not modified by the user) when idempotence is");
                builder.AppendLine("    ///     enabled: <see cref=\"KafkaClientConfiguration{TClientConfig}.MaxInFlight\" /> to 5 (must be less than or equal to 5),");
                builder.AppendLine("    ///     <see cref=\"MessageSendMaxRetries\" /> to <c>Int32.MaxValue</c> (must be greater than 0),");
                builder.AppendLine("    ///     <see cref=\"KafkaClientConfiguration{TClientConfig}.Acks\" /> to <see cref=\"Acks.All\" />. The producer instantiation will fail if user-supplied configuration");
                builder.AppendLine("    ///     is incompatible.");
                break;
            case nameof(ProducerConfig.EnableGaplessGuarantee):
                builder.AppendLine("    ///     Gets a value indicating whether an error that could result in a gap in the produced message series when a batch of messages fails,");
                builder.AppendLine("    ///     must raise a fatal error (ERR_GAPLESS_GUARANTEE) and stop the producer. Messages failing due to <see cref=\"MessageTimeoutMs\" /> are");
                builder.AppendLine("    ///     not covered by this guarantee. Requires <see cref=\"EnableIdempotence\" />=true.");
                break;
            case nameof(ProducerConfig.QueueBufferingMaxMessages):
                builder.AppendLine("    ///     Gets the maximum number of messages allowed on the producer queue. This queue is shared by all topics and partitions.");
                break;
            case nameof(ProducerConfig.QueueBufferingMaxKbytes):
                builder.AppendLine("    ///     Gets the maximum total message size sum allowed on the producer queue. This queue is shared by all topics and partitions. This");
                builder.AppendLine("    ///     property has higher priority than <see cref=\"QueueBufferingMaxMessages\" />.");
                break;
            case nameof(ProducerConfig.LingerMs):
                builder.AppendLine("    ///     Gets the delay in milliseconds to wait for messages in the producer queue to accumulate before constructing message batches to");
                builder.AppendLine("    ///     transmit to brokers. A higher value allows larger and more effective (less overhead, improved compression) batches of messages to");
                builder.AppendLine("    ///     accumulate at the expense of increased message delivery latency.");
                break;
            case nameof(ProducerConfig.MessageSendMaxRetries):
                builder.AppendLine("    ///     Gets how many times to retry sending a failing message.<br />");
                builder.AppendLine("    ///     Note: retrying may cause reordering unless <see cref=\"EnableIdempotence\" /> is set to <c>true</c>.");
                break;
            case nameof(ProducerConfig.RetryBackoffMs):
                builder.AppendLine("    ///     Gets the backoff time in milliseconds before retrying a request.");
                break;
            case nameof(ProducerConfig.QueueBufferingBackpressureThreshold):
                builder.AppendLine("    ///     Gets the threshold of outstanding not yet transmitted broker requests needed to backpressure the producer's message accumulator.");
                builder.AppendLine("    ///     If the number of not yet transmitted requests equals or exceeds this number, produce request creation that would have otherwise");
                builder.AppendLine("    ///     been triggered (for example, in accordance with <see cref=\"LingerMs\" />) will be delayed. A lower number yields larger and more");
                builder.AppendLine("    ///     effective batches. A higher value can improve latency when using compression on slow machines.");
                break;
            case nameof(ProducerConfig.CompressionType):
                builder.AppendLine("    ///     Gets the compression codec to be used to compress message sets. This is the default value for all topics, may be overridden by the");
                builder.AppendLine("    ///     topic configuration property <c>compression.codec</c>.");
                break;
            case nameof(ProducerConfig.BatchNumMessages):
                builder.AppendLine("    ///     Gets the maximum number of messages batched in one message set. The total message set size is also limited by <see cref=\"BatchSize\" />");
                builder.AppendLine("    ///     and <see cref=\"KafkaClientConfiguration{TClientConfig}.MessageMaxBytes\" />.");
                break;
            case nameof(ProducerConfig.BatchSize):
                builder.AppendLine("    ///     Gets the maximum size (in bytes) of all messages batched in one message set, including the protocol framing overhead. This limit");
                builder.AppendLine("    ///     is applied after the first message has been added to the batch, regardless of the first message size, this is to ensure that messages");
                builder.AppendLine("    ///     that exceed the <see cref=\"BatchSize\" /> are still produced. The total message set size is also limited by <see cref=\"BatchNumMessages\" />");
                builder.AppendLine("    ///     and <see cref=\"KafkaClientConfiguration{TClientConfig}.MessageMaxBytes\" />.");
                break;
            case nameof(ProducerConfig.StickyPartitioningLingerMs):
                builder.AppendLine("    ///     Gets the delay in milliseconds to wait to assign new sticky partitions for each topic. By default this is set to double the time");
                builder.AppendLine("    ///     of <see cref=\"LingerMs\" />. To disable sticky behavior, set it to 0. This behavior affects messages with the key <c>null</c> in all");
                builder.AppendLine("    ///     cases, and messages with key lengths of zero when the <see cref=\"Confluent.Kafka.Partitioner.ConsistentRandom\" /> partitioner is in");
                builder.AppendLine("    ///     use. These messages would otherwise be assigned randomly. A higher value allows for more effective batching of these messages.");
                break;
        }
    }

    private static void AppendSummaryForSchemaRegistryConfig(StringBuilder builder, PropertyInfo property)
    {
        switch (property.Name)
        {
            case nameof(SchemaRegistryConfig.BasicAuthCredentialsSource):
                builder.AppendLine("    ///     Gets the source of the basic authentication credentials. This specifies whether the credentials are specified in the <see cref=\"BasicAuthUserInfo\" />");
                builder.AppendLine("    ///     or they are inherited from the producer or consumer configuration.");
                break;
            case nameof(SchemaRegistryConfig.Url):
                builder.AppendLine("    ///     Gets the comma-separated list of URLs for schema registry instances that are used to register or lookup schemas.");
                break;
            case nameof(SchemaRegistryConfig.RequestTimeoutMs):
                builder.AppendLine("    ///     Gets the timeout in milliseconds for the requests to the Confluent schema registry.");
                break;
            case nameof(SchemaRegistryConfig.SslCaLocation):
                builder.AppendLine("    ///     Gets the file or directory path to the CA certificate(s) for verifying the registry's key. Defaults: On Windows the system's CA certificates are automatically looked up in the Windows Root certificate store.");
                builder.AppendLine("    ///     On Mac OSX this configuration defaults to <c>probe</c>. It is recommended to install openssl using Homebrew, to provide CA certificates. On Linux install the distribution's ca-certificates package.");
                builder.AppendLine("    ///     If OpenSSL is statically linked or <see cref=\"SslCaLocation\" /> is set to <c>probe</c> a list of standard paths will be probed and the first one found will be used as the default CA certificate location path.");
                builder.AppendLine("    ///     If OpenSSL is dynamically linked the OpenSSL library's default path will be used (see <c>OPENSSLDIR</c> in <c>openssl version -a</c>).");
                break;
            case nameof(SchemaRegistryConfig.SslKeystoreLocation):
                builder.AppendLine("    ///     Gets the path to the client's keystore (PKCS#12) used for the authentication.");
                break;
            case nameof(SchemaRegistryConfig.SslKeystorePassword):
                builder.AppendLine("    ///     Gets the client's keystore (PKCS#12) password.");
                break;
            case nameof(SchemaRegistryConfig.EnableSslCertificateVerification):
                builder.AppendLine("    ///     Gets a value indicating whether the registry (server) certificate must be verified.");
                break;
            case nameof(SchemaRegistryConfig.MaxCachedSchemas):
                builder.AppendLine("    ///     Gets the maximum number of schemas that are cached by the schema registry client.");
                break;
            case nameof(SchemaRegistryConfig.BasicAuthUserInfo):
                builder.AppendLine("    ///     Gets the basic authentication credentials in the form {username}:{password}.");
                break;
        }
    }
}
