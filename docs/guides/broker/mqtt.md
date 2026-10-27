---
uid: mqtt
---

# MQTT Specifics

This guide covers the features and configuration options that are specific to the MQTT integration. It complements the general guides on <xref:broker>, <xref:producing> and <xref:consuming>.

Silverback uses [MQTTnet](https://github.com/dotnet/MQTTnet) under the hood. Reference the `Silverback.Integration.MQTT` package and register the broker with `AddMqtt()`.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddMqtt())
    .AddMqttClients(clients => clients
        .ConnectViaTcp("localhost")
        .AddClient("my-client", client => client
            .WithClientId("client1")
            .Produce<MyMessage>("producer-endpoint", endpoint => endpoint
                .ProduceTo("messages/my")
                .WithAtLeastOnceQoS())
            .Consume<MyOtherMessage>("consumer-endpoint", endpoint => endpoint
                .ConsumeFrom("messages/other")
                .WithAtLeastOnceQoS())));
```

Unlike Kafka, where producers and consumers are separate entities, an MQTT client can both produce and consume. All the settings configured at the `AddMqttClients` level (connection, credentials, protocol version, etc.) are shared by all the clients and can be overridden per client in `AddClient`.

## Protocol Version

Silverback uses MQTT 5 by default. Use `UseProtocolVersion` to connect to a broker using an older protocol version.

```csharp
.AddMqttClients(clients => clients
    .ConnectViaTcp("localhost")
    .UseProtocolVersion(MqttProtocolVersion.V311)
    .AddClient("my-client", client => client
        .WithClientId("client1")
        .Consume<MyMessage>("endpoint1", endpoint => endpoint
            .ConsumeFrom("messages/my"))))
```

Many features described in this guide, such as headers (user properties), message expiration, response topic and correlation data, session expiration and last will delay, are only available with MQTT 5. See [MQTT 3.1.1 Limitations](#mqtt-311-limitations).

## Connection

### Transport

Connect via TCP, specifying the host and optionally the port (the default MQTT port is used if omitted):

```csharp
.AddMqttClients(clients => clients
    .ConnectViaTcp("localhost", 1883)
    .AddClient("my-client", client => client
        .WithClientId("client1")
        .Consume<MyMessage>("endpoint1", endpoint => endpoint
            .ConsumeFrom("messages/my"))))
```

An overload accepts an action to configure further TCP options via the <xref:Silverback.Messaging.Configuration.Mqtt.MqttClientTcpConfigurationBuilder> (`WithBufferSize`, `EnableNoDelay`, `WithAddressFamily`, etc.).

Connect via WebSocket, optionally through a proxy:

```csharp
.AddMqttClients(clients => clients
    .ConnectViaWebSocket(
        "ws://localhost:8080/mqtt",
        webSocket => webSocket
            .UseProxy(
                "http://proxy:3128",
                proxy => proxy
                    .WithCredentials("proxy-user", "proxy-password")
                    .EnableBypassOnLocal())))
```

Alternatively, `ConnectTo` accepts a server URI and configures the transport according to its scheme (`tcp://`, `mqtt://`, `mqtts://` for TCP with TLS, `ws://` and `wss://` for WebSocket). The credentials can be specified as user info in the URI.

```csharp
.AddMqttClients(clients => clients
    .ConnectTo("mqtts://my-broker.example.com:8883"))
```

### TLS

Enable TLS with `EnableTls`, optionally configuring it via the <xref:Silverback.Messaging.Configuration.Mqtt.MqttClientTlsConfigurationBuilder>:

```csharp
.AddMqttClients(clients => clients
    .ConnectViaTcp("my-broker.example.com", 8883)
    .EnableTls(tls => tls
        .WithSslProtocol(SslProtocols.Tls12 | SslProtocols.Tls13)
        .WithTrustChain(caCertificate)
        .WithClientCertificatesProvider(myCertificatesProvider)))
```

The TLS builder also exposes `WithTargetHost`, `WithCertificateValidationHandler`, `WithRevocationMode`, `IgnoreCertificateChainErrors`, `AllowUntrustedCertificates`, and more.

> [!Warning]
> `AllowUntrustedCertificates()`, `IgnoreCertificateChainErrors()` and `IgnoreCertificateRevocationErrors()` weaken the security of the connection and should only be used in development environments.

### Credentials and Authentication

```csharp
.AddMqttClients(clients => clients
    .ConnectViaTcp("localhost")
    .WithCredentials("my-user", "my-password"))
```

`WithCredentials` also accepts the password as `byte[]` or an `IMqttClientCredentialsProvider`, to resolve the credentials when connecting. For MQTT 5 enhanced authentication, use `WithEnhancedAuthentication(method, data)` or `UseEnhancedAuthenticationHandler<THandler>()`.

### Client Identifier

Each client is identified by its client id, set with `WithClientId`.

```csharp
.AddClient("my-client", client => client
    .WithClientId("my-service-1"))
```

If not specified, a random identifier is generated each time the application starts.

> [!Important]
> The broker allows a single connection per client id: when a second client connects with the same identifier, the first one is disconnected. If you scale out your application, make sure that each instance uses a unique client id (and see [Shared Subscriptions](#shared-subscriptions) to distribute the load).
>
> On the other hand, the client id must be stable across restarts to resume a [persistent session](#persistent-sessions).

### Other Connection Settings

- `SendKeepAlive(TimeSpan)` / `DisableKeepAlive()`: configures the keep alive period.
- `WithTimeout(TimeSpan)`: sets the timeout applied at socket level and to the internal operations.
- `WithAcknowledgmentTimeout(TimeSpan)`: sets the maximum time to wait for an acknowledgment operation to complete (default 30 seconds).
- `LimitUnacknowledgedPublications(int)`: sets the MQTT 5 *Receive Maximum*, the maximum number of QoS 1 and 2 messages the broker can send to the client without receiving an acknowledgment.
- `LimitPacketSize(long)`: sets the maximum packet size the client is willing to accept.
- `AddUserProperty(name, value)`: adds a user property to the `CONNECT` packet (not to the produced messages).
- `RequestProblemInformation()` / `RequestResponseInformation()`: request the corresponding MQTT 5 information from the broker.

## Quality of Service

The quality of service (QoS) level is configured per endpoint, on both the producer and the consumer side:

- `WithAtMostOnceQoS()`: QoS 0, fire and forget (default).
- `WithAtLeastOnceQoS()`: QoS 1, the message is delivered at least once and could be duplicated.
- `WithExactlyOnceQoS()`: QoS 2, the message is delivered exactly once.
- `WithQualityOfServiceLevel(MqttQualityOfServiceLevel)`: sets the level explicitly.

```csharp
.AddClient("my-client", client => client
    .WithClientId("client1")
    .Produce<MyMessage>("producer-endpoint", endpoint => endpoint
        .ProduceTo("messages/my")
        .WithExactlyOnceQoS())
    .Consume<MyOtherMessage>("consumer-endpoint", endpoint => endpoint
        .ConsumeFrom("messages/other")
        .WithAtLeastOnceQoS()))
```

On the consumer side the QoS is the maximum level requested in the subscription: the messages are delivered with the lower of the QoS used to publish them and the QoS of the subscription.

> [!Important]
> The default QoS is 0 (at most once), which provides no delivery guarantees. Use QoS 1 or 2 if you can't afford to lose messages, especially in combination with [persistent sessions](#persistent-sessions) and [error policies](xref:error-policies).

## Producing

### Retain

Use `Retain()` to set the retain flag on the produced messages. The broker stores the last retained message of each topic and delivers it to any client subscribing later.

```csharp
.Produce<DeviceStatus>("status", endpoint => endpoint
    .ProduceTo("devices/status")
    .WithAtLeastOnceQoS()
    .Retain())
```

### Message Expiration

In MQTT 5, use `WithMessageExpiration(TimeSpan)` to let the broker discard the message if it hasn't been delivered within the specified interval.

```csharp
.Produce<MyMessage>("producer-endpoint", endpoint => endpoint
    .ProduceTo("messages/my")
    .WithMessageExpiration(TimeSpan.FromMinutes(5)))
```

### No Matching Subscribers

An MQTT 5 broker can notify the client when a QoS 1 or 2 message was published to a topic without any matching subscriber. By default, Silverback ignores this notification. You can configure the behavior per endpoint:

- `IgnoreNoMatchingSubscribers()`: ignores the notification (default).
- `LogNoMatchingSubscribersWarning()`: logs a warning.
- `ThrowNoMatchingSubscribersError()`: throws an `MqttProduceException`, causing the produce operation to fail.

```csharp
.Produce<MyMessage>("producer-endpoint", endpoint => endpoint
    .ProduceTo("messages/my")
    .WithAtLeastOnceQoS()
    .ThrowNoMatchingSubscribersError())
```

### Dynamic Topics

As with Kafka, the destination topic can be computed per message, using a function, a resolver class (`UseEndpointResolver<TResolver>()`) or the `SetMqttDestinationTopic` envelope extension in combination with `ProduceToDynamicTopic()`. See the routing section in <xref:producing>.

```csharp
.Produce<SensorReading>("readings", endpoint => endpoint
    .ProduceTo(reading => $"sensors/{reading?.SensorId}/readings")
    .WithAtLeastOnceQoS())
```

## Consuming

### Topic Filters and Wildcards

`ConsumeFrom` accepts one or more topics or topic filters, including the MQTT wildcards:

- `+` matches exactly one topic level (e.g. `sensors/+/readings` matches `sensors/1/readings`, but not `sensors/1/2/readings`).
- `#` matches any number of levels and must be the last character (e.g. `sensors/#`).

```csharp
.Consume<SensorReading>("readings", endpoint => endpoint
    .ConsumeFrom("sensors/+/readings", "legacy/sensors/#")
    .WithAtLeastOnceQoS())
```

Each received message is matched to the endpoint whose topic filters match its topic. The actual topic of a message consumed through a wildcard subscription is available via the envelope endpoint:

```csharp
public Task HandleAsync(IInboundEnvelope<SensorReading> envelope)
{
    string topic = envelope.Endpoint.RawName;
    // ...
}
```

> [!Note]
> The same topic can't be subscribed by multiple endpoints in the same client. Use separate clients if you need to subscribe the same topic with a different configuration.

### Shared Subscriptions

MQTT 5 shared subscriptions allow multiple clients to share the load of a subscription: each message is delivered to only one of the clients in the group. Use the `$share/{group}/{filter}` syntax:

```csharp
.AddClient("my-client", client => client
    .WithClientId($"my-service-{instanceId}")
    .Consume<MyMessage>("endpoint1", endpoint => endpoint
        .ConsumeFrom("$share/my-service/messages/my")
        .WithAtLeastOnceQoS()))
```

Silverback strips the `$share/{group}/` prefix when matching the received messages to the endpoint.

### Parallel Processing and Backpressure

By default, the messages consumed by a client are processed sequentially, one at a time. Use `EnableParallelProcessing` to process multiple messages concurrently.

```csharp
.AddClient("my-client", client => client
    .WithClientId("client1")
    .EnableParallelProcessing(10)
    .LimitBackpressure(5)
    .Consume<MyMessage>("endpoint1", endpoint => endpoint
        .ConsumeFrom("messages/topic1", "messages/topic2")
        .WithAtLeastOnceQoS()))
```

Use `LimitBackpressure(n)` to limit how many incoming messages Silverback buffers per parallel execution. The total buffer capacity per client is `n × MaxDegreeOfParallelism`, excluding the messages already being processed. The default backpressure limit is 2. This setting controls Silverback's processing buffers, not the buffers in MQTTnet or in the broker.

> [!Warning]
> With parallel processing enabled, the messages are distributed across the parallel executions and the processing order is no longer guaranteed, not even for messages coming from the same topic.

Both settings apply to the whole client, not to a single endpoint. Use `DisableParallelProcessing()` to restore the sequential processing.

## Headers (User Properties)

In MQTT 5, the message headers are mapped to user properties. This includes the headers set via `AddHeader` or the `[Header]` attribute and the headers added by Silverback itself (e.g. `x-message-type` or the tracing headers). See <xref:producing-headers>, <xref:consuming-headers> and <xref:default-headers>.

### Response Topic and Correlation Data

MQTT 5 defines a response topic and correlation data as first-class message properties, to implement request-response patterns. Silverback maps them to the internal headers defined in <xref:Silverback.Messaging.Messages.MqttMessageHeaders>, which aren't sent as user properties.

Set them statically or per message in the producer endpoint configuration:

```csharp
.Produce<MyRequest>("requests", endpoint => endpoint
    .ProduceTo("requests/my")
    .SetResponseTopic("responses/my-service")
    .SetCorrelationData(request => request?.RequestId.ToString()))
```

Or via the envelope extensions when publishing:

```csharp
await publisher.WrapAndPublishAsync(
    new MyRequest(),
    envelope => envelope
        .SetMqttResponseTopic("responses/my-service")
        .SetMqttCorrelationData(correlationId));
```

On the consumer side, read them with the `GetMqttResponseTopic`, `GetMqttCorrelationData` and `GetMqttCorrelationDataAsString` envelope extensions:

```csharp
public async Task HandleAsync(IInboundEnvelope<MyRequest> envelope)
{
    string? responseTopic = envelope.GetMqttResponseTopic();
    byte[]? correlationData = envelope.GetMqttCorrelationData();
    // ...
}
```

See also <xref:mqtt-request-response>.

## Last Will Message

The last will (or testament) message is sent by the broker on behalf of the client when it disconnects ungracefully (e.g. on network failure or if the keep alive expires). Configure it with `SendLastWillMessage`:

```csharp
.AddClient("my-client", client => client
    .WithClientId("client1")
    .SendLastWillMessage<TestamentMessage>(lastWill => lastWill
        .SendMessage(new TestamentMessage { ClientId = "client1" })
        .ProduceTo("testaments")
        .WithAtLeastOnceQoS()
        .Retain()
        .WithDelay(TimeSpan.FromSeconds(30))
        .WithExpiration(TimeSpan.FromHours(1))))
```

- `SendMessage` and `ProduceTo` (required) set the message and the destination topic.
- `WithAtMostOnceQoS()`, `WithAtLeastOnceQoS()`, `WithExactlyOnceQoS()` or `WithQualityOfServiceLevel(...)` set the QoS.
- `Retain()` sets the retain flag.
- `WithDelay(TimeSpan)` (MQTT 5) delays the sending of the message; it isn't sent if the client reconnects in the meantime.
- `WithExpiration(TimeSpan)` (MQTT 5) sets the message expiry interval.
- `AddHeader(name, value)` (MQTT 5) adds a user property.
- `WithContentType`, `WithCorrelationData` and `WithPayloadFormatIndicator` set the corresponding MQTT 5 properties.
- The message is serialized as JSON by default; use `SerializeAsJson(...)` or `SerializeUsing(...)` to customize the serialization.

## Persistent Sessions

Use `RequestPersistentSession()` to resume the broker session after reconnecting. Configure a stable client identifier with `WithClientId(...)`; reconnecting with a different identifier creates a different session. With MQTT 5, persistence requests no expiration by default, unless an expiration was explicitly configured. For a finite retention period, use `RequestPersistentSession(TimeSpan.FromHours(1))` or `WithSessionExpiration(TimeSpan.FromHours(1))`. Shared client settings also support `RequestPersistentSession()` and `WithSessionExpiration(...)`.

```csharp
.AddClient("my-client", client => client
    .WithClientId("my-service-1")
    .RequestPersistentSession(TimeSpan.FromHours(1))
    .Consume<MyMessage>("endpoint1", endpoint => endpoint
        .ConsumeFrom("messages/my")
        .WithAtLeastOnceQoS()))
```

`WithSessionExpiration(TimeSpan.Zero)` explicitly ends the session on disconnect, even when used with `RequestPersistentSession()`. `TimeSpan.MaxValue` requests no expiration. Finite durations are rounded up to whole seconds, up to 4,294,967,294 seconds. The broker can override the requested expiration. MQTT 3.1.1 uses its persistent-session flag and does not support a client-configured expiration.

`RequestCleanSession()` discards any existing session and requests a nonpersistent session. In MQTT 5, `RequestCleanStart()` discards the existing session but preserves the configured expiration, so the new session can persist after disconnect.

Unread messages buffered locally are discarded on disconnect. Broker replay requires a persistent session and QoS 1 or 2; QoS 0 does not provide replay guarantees. Subscribers must still tolerate repeated processing when an unfinished message is redelivered.

## Error Handling

The error policies work with MQTT as with Kafka, with some differences: MQTT can't re-read a message from the broker, so the retry policy reprocesses the message held locally, which is acknowledged only once it has been successfully processed, skipped or moved. See <xref:error-policies> for the details.

## MQTT 3.1.1 Limitations

MQTT 3.1.1 doesn't support user properties, therefore no headers are transmitted with the messages. This has some consequences:

- The message type can't be sent in the `x-message-type` header, so the consumer must know the type of the messages in advance. Use a concrete class as message type in the consumer endpoint (e.g. `Consume<MyMessage>`): Silverback rejects the configuration if the JSON deserializer is bound to `object` or to an interface, since it would require the message type header.
- Custom headers and the tracing headers are dropped.
- The `x-failed-attempts` header can't be transferred with the messages moved by the `MoveTo` error policy, therefore a maximum number of attempts can't be set on error policies other than `Retry`.
- Message expiration, response topic, correlation data, session expiration, last will delay and the other MQTT 5 properties aren't available.

> [!Note]
> Chunking is currently not supported for MQTT, regardless of the protocol version.

## Additional Resources

- [API Reference](xref:Silverback)
- <xref:broker>
- <xref:producing>
- <xref:consuming>
- <xref:error-policies>
- <xref:default-headers>
- <xref:mqtt-request-response>
- [MQTTnet](https://github.com/dotnet/MQTTnet/wiki)
