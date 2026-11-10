---
uid: mqtt-message-properties
description: "Set MQTT-specific producer options: the retain flag, message expiration, response topic and correlation data, and handling of no matching subscribers."
---

# MQTT Message Properties

This guide covers the MQTT-specific options of the producer endpoints. See <xref:mqtt> for the client configuration and the options shared by the producer and consumer endpoints, such as the quality of service, and <xref:producing> for the general producer configuration, including the dynamic topics.

## Retain

Use `Retain()` to set the retain flag on the produced messages. The broker stores the last retained message of each topic and delivers it to any client subscribing later.

```csharp
.Produce<DeviceStatus>("status", endpoint => endpoint
    .ProduceTo("devices/status")
    .WithAtLeastOnceQoS()
    .Retain())
```

## Message Expiration

In MQTT 5, use `WithMessageExpiration(TimeSpan)` to let the broker discard the message if it hasn't been delivered within the specified interval.

```csharp
.Produce<MyMessage>("producer-endpoint", endpoint => endpoint
    .ProduceTo("messages/my")
    .WithMessageExpiration(TimeSpan.FromMinutes(5)))
```

## Response Topic and Correlation Data

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

## No Matching Subscribers

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

## Additional Resources

- [API Reference](xref:Silverback)
- <xref:producing>
- <xref:mqtt>
- <xref:mqtt-request-response>
