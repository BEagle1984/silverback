---
uid: mqtt-subscription
description: "Subscribe MQTT consumers with topic filters and wildcards or shared subscriptions, and see how parallel processing, backpressure and redelivery work."
---

# MQTT Subscription and Processing

This guide covers how the MQTT consumer endpoints subscribe their topics and how the received messages are processed. See <xref:mqtt> for the client configuration, such as the persistent sessions, and the options shared by the producer and consumer endpoints, such as the quality of service.

## Topic Filters and Wildcards

`ConsumeFrom` accepts one or more topics or topic filters, including the MQTT wildcards:

- `+` matches exactly one topic level (e.g. `sensors/+/readings` matches `sensors/1/readings`, but not `sensors/1/2/readings`).
- `#` matches any number of levels, including the parent level, and must be the last level (e.g. `sensors/#` matches `sensors`, `sensors/1` and `sensors/1/readings`).

Each wildcard must occupy an entire level: Silverback rejects an invalid filter such as `sensors/#/readings` or `sensors+` when validating the configuration. As defined by the MQTT specification, the filters starting with a wildcard (e.g. `#` or `+/readings`) don't match the topics starting with `$`, such as `$SYS/...`.

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
> The same topic can't be subscribed by multiple endpoints in the same client, nor more than once through shared subscriptions (e.g. `orders` and `$share/group/orders`, or `$share/group1/orders` and `$share/group2/orders`), since the broker delivers a copy of each message per subscription. Use separate clients if you need to subscribe the same topic with a different configuration.

## Shared Subscriptions

MQTT 5 shared subscriptions allow multiple clients to share the load of a subscription: each message is delivered to only one of the clients in the group. Use the `$share/{group}/{filter}` syntax:

```csharp
.AddClient("my-client", client => client
    .WithClientId($"my-service-{instanceId}")
    .Consume<MyMessage>("endpoint1", endpoint => endpoint
        .ConsumeFrom("$share/my-service/messages/my")
        .WithAtLeastOnceQoS()))
```

Silverback strips the `$share/{group}/` prefix when matching the received messages to the endpoint.

## Rejected Subscriptions

If the broker rejects a subscription (e.g. because the client isn't authorized to subscribe the topic), Silverback logs an error (`SubscriptionRejected`) and the other topic filters of the client remain subscribed. If all the topic filters are rejected, the consumer isn't reported as connected. The rejected topic filters are subscribed again only when the client reconnects.

## Parallel Processing and Backpressure

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

## Redelivery and Error Handling

A message is acknowledged only once it has been successfully processed, skipped or moved. The broker redelivers the unacknowledged messages after a reconnection only with QoS 1 or 2 and a [persistent session](xref:mqtt#persistent-sessions), so the subscribers must tolerate processing the same message more than once.

The error policies work with MQTT as with Kafka, with some differences: MQTT can't re-read a message from the broker, so the retry policy reprocesses the message held locally. While a message is being retried, the processing channel it belongs to is blocked. With the default sequential processing, the whole client stops processing messages. With parallel processing, the other channels keep processing only until the buffer of the blocked channel is full, since the messages are distributed round-robin, or until the *Receive Maximum* is reached. See <xref:error-policies> for the details.

## Additional Resources

- [API Reference](xref:Silverback)
- <xref:consuming>
- <xref:mqtt>
- <xref:error-policies>
