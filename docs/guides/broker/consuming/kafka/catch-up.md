---
uid: kafka-catch-up
description: "Consume all Kafka partitions assigned to a consumer until their end and get notified once all messages are processed, for example to load the in-memory state of a service before it starts."
---

# Kafka Catch-Up

Some topics must be fully consumed before the application, or part of it, can start: for example, to load the in-memory state of a service. A consumer can catch up with the partitions assigned to it: it consumes each partition until its end and notifies once all of them are caught up, meaning that they reached their end and all their messages are processed.

## Enabling the Catch-Up

Use `NotifyWhenCaughtUp` to keep consuming once caught up, or `DisconnectWhenCaughtUp` to disconnect the consumer. Combined with `ConsumeFromBeginning` (see [Static Assignment from the Beginning](xref:kafka-subscription#static-assignment-from-the-beginning)) and `DisableOffsetsCommit`, the consumer reads the whole topic every time it connects, without a group id.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("state-loader", consumer => consumer
            .DisconnectWhenCaughtUp()
            .DisableOffsetsCommit()
            .Consume<StateEvent>(endpoint => endpoint
                .ConsumeFromBeginning("state-topic"))));
```

The catch-up works with consumer groups as well: it then covers the partitions assigned to that consumer only, and the consumer is caught up only once at least one partition is assigned to it. A member joining a group may receive an empty assignment first and its partitions in a later rebalance (e.g. with the cooperative sticky assignor).

## Getting Notified

Implement an <xref:Silverback.Messaging.Broker.Callbacks.IKafkaConsumerCaughtUpCallback> (see <xref:broker-callbacks>) to be notified when the consumer is caught up.

```csharp
public class StateLoadedCallback : IKafkaConsumerCaughtUpCallback
{
    public Task OnConsumerCaughtUpAsync(IKafkaConsumer consumer)
    {
        // The state is loaded
        return Task.CompletedTask;
    }
}
```

Alternatively, await <xref:Silverback.Messaging.Broker.IKafkaConsumer.WaitUntilCaughtUpAsync(System.Threading.CancellationToken)>, for example in a hosted service that must not start before the state is loaded. The returned task completes before the callbacks are invoked.

> [!Important]
> Awaiting the catch-up in `IHostedService.StartAsync`, as in the example below, blocks the startup of the following hosted services. It works with the default connection mode (`BrokerClientConnectionMode.Startup`, see <xref:broker>), if the hosted service is registered after Silverback. Otherwise, the consumer is connected only after the startup, and `StartAsync` would wait forever: await the catch-up in a `BackgroundService` instead.

```csharp
public class StartupService : IHostedService
{
    private readonly IConsumerCollection _consumers;

    public StartupService(IConsumerCollection consumers)
    {
        _consumers = consumers;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        IKafkaConsumer consumer = (IKafkaConsumer)_consumers["state-loader"];
        await consumer.WaitUntilCaughtUpAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

## Behavior

* The consumer is caught up when every partition assigned to it reached its end and all the messages before the end are processed (or skipped by an error policy). The end is reached when the consumer catches up with the messages written so far: a consumer that can't keep up with the producers never catches up (see also the end of partition limitation below).
* A pending batch is completed as soon as the end of the partition is reached, without waiting for it to be full or for its `MaxWaitTime`. A stream is considered caught up once its messages are read. A partition ending with an incomplete chunked message is caught up once the messages before it are processed: the chunked message is consumed once complete.
* The messages filtered out or unhandled at the end of a batch don't prevent the consumer from catching up, even though they're committed only with the next batch.
* The partitions assigned to the consumer once it's caught up are ignored, and the notification is sent once per connection: each reconnect starts a new catch-up, while stopping and starting the consumer continues the same one.
* With a consumer group, a partition revoked by a rebalance before it's caught up is dropped from the catch-up, which then covers only the partitions the consumer keeps. A member that never gets a partition never catches up: pass a cancellation token to `WaitUntilCaughtUpAsync` if it may happen.
* With `DisconnectWhenCaughtUp`, each partition is paused as soon as it reaches its end (a few messages written meanwhile may still be processed), and the consumer is disconnected after the callbacks are invoked. Connect it again (via `IBrokerClient.ConnectAsync`) to start a new catch-up, from the committed offsets or, with a static assignment, from the configured offsets (unless a client side offset store is used).
* The catch-up relies on the end of partition events, which are enabled regardless of `EnablePartitionEof`. The <xref:Silverback.Messaging.Broker.Callbacks.IKafkaPartitionEofCallback> is therefore invoked as well.

> [!Note]
> The consumer is disconnected rather than stopped, because a stopped consumer doesn't poll: in a consumer group, it would keep its partitions unconsumed until `max.poll.interval.ms` elapses and it's removed from the group.

> [!Important]
> The end of a partition is reported only when a fetch request returns no new message. The broker holds each fetch request until some data is available, for up to `fetch.wait.max.ms` (500 ms by default), so a partition receiving a message more often than that may never report its end, even though the consumer keeps up with it, and the consumer never catches up. If the topic is written continuously, lower `fetch.wait.max.ms` via `WithFetchWaitMaxMs` (e.g. to 50 ms), at the cost of more fetch requests.

## Additional Resources

- [API Reference](xref:Silverback)
- <xref:kafka-subscription>
- <xref:broker-callbacks>
