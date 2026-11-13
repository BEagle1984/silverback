---
uid: testing
description: "Test Kafka and MQTT applications with in-memory mocked brokers, testing helpers that wait for consumption and the integration spy to inspect messages."
---

# Testing

Testing event-driven applications can be tricky because producing and consuming messages is asynchronous and typically involves external infrastructure (Kafka, MQTT, ...).

Silverback ships dedicated **testing packages** that replace the broker connectivity with **in-memory mocked brokers** and exposes a set of **helpers** to make end-to-end testing deterministic.

## Packages

Choose the package matching the broker you’re using:

- [Silverback.Integration.Kafka.Testing](https://www.nuget.org/packages/Silverback.Integration.Kafka.Testing)
  - Mocked in-memory Kafka broker (topics, consumer groups, commits, ...).
  - Registers <xref:Silverback.Testing.IKafkaTestingHelper>.
- [Silverback.Integration.MQTT.Testing](https://www.nuget.org/packages/Silverback.Integration.MQTT.Testing)
  - Mocked in-memory MQTT broker.
  - Registers <xref:Silverback.Testing.IMqttTestingHelper>.
- [Silverback.Integration.Testing](https://www.nuget.org/packages/Silverback.Integration.Testing)
  - Shared testing infrastructure (e.g. <xref:Silverback.Testing.IIntegrationSpy> and <xref:Silverback.Testing.ITestingHelper>).
  - This package is referenced by the broker-specific testing packages.

## Two Ways to Use the Mocked Broker

There are two supported approaches and they solve different problems:

1. **Test-only configuration**: your test registers the broker using `AddMockedKafka` / `AddMockedMqtt`.
   This is the simplest approach when your test builds the `ServiceCollection` (or you fully control the test host setup).

2. **Override an existing configuration**: your application configures a real broker in `Startup` / `Program`,
   and your test host overrides that by calling `UseMockedKafka` / `UseMockedMqtt` in `ConfigureTestServices` (or equivalent).
   This is ideal when the app’s configuration is shared between production and tests, and you just want to swap the transport.

> [!Tip]
> You don’t need both. `AddMockedKafka` and `AddMockedMqtt` already register the broker and replace the transport.
> `UseMockedKafka` / `UseMockedMqtt` are for *overriding* an existing setup.

## Approach 1: Test-Only Configuration (Recommended When Building the DI Container)

### Kafka (AddMockedKafka)

```csharp
services
    .AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
    .AddKafkaClients(
        clients => clients
            .WithBootstrapServers("PLAINTEXT://tests")
            .AddProducer(
                producer => producer
                    .Produce<SomeMessage>(endpoint => endpoint.ProduceTo("test-topic")))
            .AddConsumer(
                consumer => consumer
                    .WithGroupId("test-consumer")
                    .Consume(endpoint => endpoint.ConsumeFrom("test-topic"))));
```

### MQTT (AddMockedMqtt)

```csharp
services
    .AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
    .AddMqttClients(
        clients => clients
            .ConnectViaTcp("tests")
            .AddClient(
                client => client
                    .WithClientId("test-client")
                    .Produce<SomeMessage>(endpoint => endpoint.ProduceTo("test/topic"))
                    .Consume(endpoint => endpoint.ConsumeFrom("test/topic"))));
```

## Approach 2: Override an Existing Configuration (WebApplicationFactory / Test Host)

If your application always configures the real broker (e.g. `AddKafka()` / `AddMqtt()` in `Program.cs`),
then in tests you can keep that configuration and only swap the connectivity implementation.

### Kafka (UseMockedKafka)

```csharp
builder.ConfigureTestServices(services =>
{
    services
        .ConfigureSilverback()
        .UseMockedKafka();
});
```

### MQTT (UseMockedMqtt)

```csharp
builder.ConfigureTestServices(services =>
{
    services
        .ConfigureSilverback()
        .UseMockedMqtt();
});
```

> [!Note]
> `UseMockedKafka` / `UseMockedMqtt` replace the underlying broker connectivity (Confluent.Kafka / MQTTnet) with an in-memory implementation.
> They don’t add producers/consumers/endpoints for you; those still come from your normal app configuration.

## Testing Helpers

The broker-specific testing helper (<xref:Silverback.Testing.IKafkaTestingHelper> or <xref:Silverback.Testing.IMqttTestingHelper>) also implements the base <xref:Silverback.Testing.ITestingHelper> interface.

The most commonly used helpers are:

- <xref:Silverback.Testing.ITestingHelper.WaitUntilConnectedAsync(System.Nullable{System.TimeSpan})> – wait until consumers are connected and ready.
- <xref:Silverback.Testing.ITestingHelper.WaitUntilAllMessagesAreConsumedAsync(System.String[])> – wait until routed messages have been processed (**mocked brokers only**).
- <xref:Silverback.Testing.IKafkaTestingHelper.WaitUntilAllMessagesAreCommittedAsync(System.String[])> – wait until Kafka message offsets have been committed (**mocked Kafka only**).
- <xref:Silverback.Testing.ITestingHelper.WaitUntilOutboxIsEmptyAsync(System.Nullable{System.TimeSpan})> – wait until the outbox has been drained (if you’re using the outbox).

### Example: Publish and Wait

This pattern is used extensively in our end-to-end tests (see `tests/Silverback.Integration.Tests.E2E`).

```csharp
await Host.ConfigureServicesAndRunAsync(
    services => services
        .AddLogging()
        .AddSilverback()
        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
        .AddKafkaClients(
            clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(
                    producer => producer
                        .Produce<SomeMessage>(endpoint => endpoint.ProduceTo("test-topic")))
                .AddConsumer(
                    consumer => consumer
                        .WithGroupId("test-group")
                        .Consume(endpoint => endpoint.ConsumeFrom("test-topic"))))
        .AddIntegrationSpyAndSubscriber());

var publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
await publisher.PublishAsync(new SomeMessage());

await Helper.WaitUntilAllMessagesAreConsumedAsync();

Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
```

> [!Important]
> Don’t assert immediately after producing/publishing. Always wait for the broker pipeline to finish.
> With mocked brokers you typically want `WaitUntilAllMessagesAreConsumedAsync()`.

## Inspecting Messages with the Integration Spy

When you need to assert what was produced/consumed (including headers, raw payload, endpoint name, etc.), enable the integration spy:

- <xref:Silverback.Configuration.SilverbackBuilderIntegrationTestingExtensions.AddIntegrationSpy(Silverback.Configuration.SilverbackBuilder,System.Boolean)> (default: monitors inbound via a broker behavior)
- <xref:Silverback.Configuration.SilverbackBuilderIntegrationTestingExtensions.AddIntegrationSpyAndSubscriber(Silverback.Configuration.SilverbackBuilder)> (monitors inbound via a generic subscriber)

```csharp
services
    .AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
    // ...clients/endpoints...
    .AddIntegrationSpyAndSubscriber();

// later in the test...
await Helper.WaitUntilAllMessagesAreConsumedAsync();

Helper.Spy.OutboundEnvelopes.ShouldNotBeEmpty();
Helper.Spy.InboundEnvelopes.ShouldNotBeEmpty();
```

## Testing with the Outbox

When the application stores the messages to the [transactional outbox](xref:outbox), the messages are produced asynchronously by the outbox worker, which makes the tests harder to write. The <xref:Silverback.Testing.IOutboxTestingHelper> (exposed by <xref:Silverback.Testing.ITestingHelper.Outbox>) and a couple of test-only configuration methods help with that.

The outboxes are discovered from the registered outbox workers and from the producer endpoints that store to the outbox, so the helper works even if the worker runs in another process. The methods reading the outbox (`GetMessagesAsync`, `GetLengthAsync`, `IsEmptyAsync` and `ClearAsync`) accept an optional <xref:Silverback.Messaging.Producing.TransactionalOutbox.OutboxSettings> to target a single outbox, otherwise all the discovered outboxes are considered. `ProcessAsync`, `PauseWorkerAsync` and `ResumeWorker` have an overload with the settings as well, but they need an outbox worker registered in the host.

> [!Important]
> `WaitUntilEmptyAsync` called without settings, as well as `WaitUntilOutboxIsEmptyAsync` and `WaitUntilAllMessagesAreConsumedAsync`, wait only for the outboxes that are actually being drained, i.e. those with a registered outbox worker that isn't paused. They don't wait if you are using `UseManualOutboxWorker`, after `PauseWorkerAsync` (or after pausing the worker service with `Pause()`), or if there is no worker in the host. Pass the settings to `WaitUntilEmptyAsync` to wait for a specific outbox regardless.

- <xref:Silverback.Testing.IOutboxTestingHelper.GetMessagesAsync(Silverback.Messaging.Producing.TransactionalOutbox.OutboxSettings)> – get the pending messages, without removing them.
- <xref:Silverback.Testing.IOutboxTestingHelper.GetLengthAsync(Silverback.Messaging.Producing.TransactionalOutbox.OutboxSettings)> and <xref:Silverback.Testing.IOutboxTestingHelper.IsEmptyAsync(Silverback.Messaging.Producing.TransactionalOutbox.OutboxSettings)> – check how many messages are pending.
- <xref:Silverback.Testing.IOutboxTestingHelper.WaitUntilEmptyAsync(System.Nullable{System.TimeSpan},Silverback.Messaging.Producing.TransactionalOutbox.OutboxSettings)> – wait until the outbox has been drained (see the note above).
- <xref:Silverback.Testing.IOutboxTestingHelper.ClearAsync(Silverback.Messaging.Producing.TransactionalOutbox.OutboxSettings,System.Threading.CancellationToken)> – remove all the pending messages without producing them.
- <xref:Silverback.Testing.IOutboxTestingHelper.ProcessAsync(System.Threading.CancellationToken)> – run the outbox worker right away, until the outbox is empty. The exceptions are thrown to the caller, including when some messages can't be produced (this relies on the built-in outbox worker to detect the failures: a custom `IOutboxWorker` is trusted to return `true` only on success). The pass is serialized with the outbox worker running in the same host, but the distributed lock is bypassed: no other process may be processing the same outbox, otherwise the messages could be produced twice. Combine it with `UseManualOutboxWorker` or `PauseWorkerAsync` to decide when the messages are produced.
- <xref:Silverback.Testing.IOutboxTestingHelper.PauseWorkerAsync(System.Threading.CancellationToken)> and <xref:Silverback.Testing.IOutboxTestingHelper.ResumeWorker> – stop and restart the regular processing performed by the outbox worker. The pause takes effect right away and completes when the worker has finished processing the current batch (canceling it only stops the wait). `ResumeWorker` also resumes the workers suspended by `UseManualOutboxWorker`.

### Manual Outbox Worker

<xref:Silverback.Messaging.Configuration.BrokerOptionsBuilderOutboxTestingExtensions.UseManualOutboxWorker(Silverback.Messaging.Configuration.BrokerOptionsBuilder)> prevents the registered outbox workers from processing the outbox on their own. The messages remain in the outbox until the test calls `Helper.Outbox.ProcessAsync()`, allowing to assert the content of the outbox and to decide when the messages are produced. It relies on the generic host (`IHostedLifecycleService`) to suspend the workers before they start.

```csharp
await Host.ConfigureServicesAndRunAsync(
    services => services
        .AddLogging()
        .AddSilverback()
        .WithConnectionToMessageBroker(
            options => options
                .AddMockedKafka()
                .AddInMemoryOutbox()
                .AddOutboxWorker(worker => worker.ProcessOutbox(outbox => outbox.UseMemory()))
                .UseManualOutboxWorker())
        .AddKafkaClients(
            clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(
                    producer => producer
                        .Produce<SomeMessage>(
                            "my-endpoint",
                            endpoint => endpoint
                                .ProduceTo("test-topic")
                                .StoreToOutbox(outbox => outbox.UseMemory())))
                .AddConsumer(
                    consumer => consumer
                        .WithGroupId("test-group")
                        .Consume(endpoint => endpoint.ConsumeFrom("test-topic"))))
        .AddIntegrationSpyAndSubscriber());

var publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
await publisher.PublishAsync(new SomeMessage());

(await Helper.Outbox.GetMessagesAsync()).Count.ShouldBe(1);

await Helper.Outbox.ProcessAsync();
await Helper.WaitUntilAllMessagesAreConsumedAsync();

Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
```

### Bypass the Outbox

<xref:Silverback.Messaging.Configuration.BrokerOptionsBuilderOutboxTestingExtensions.BypassOutbox(Silverback.Messaging.Configuration.BrokerOptionsBuilder)> replaces the outbox writer with one that produces the messages to the broker right away, instead of storing them, and the outbox reader with one that is always empty. No outbox worker and no outbox storage are needed in the test, and the outbox remains empty.

> [!Note]
> The database transaction (if any) is ignored and the produce errors are thrown to the publisher.
> Call `BypassOutbox` inside the `WithConnectionToMessageBroker` callback, after any other method overriding the outbox, such as `UseInMemoryOutbox`: the last override wins.
> If an outbox worker is registered anyway, its distributed lock may still use the database.

## Kafka Specifics

### Auto-Commit Interval

`WaitUntilAllMessagesAreConsumedAsync()` uses locally stored offsets, so it does not depend on the auto-commit interval or require broker commits to be enabled. Use `WaitUntilAllMessagesAreCommittedAsync()` when asserting committed offsets.

Mocked Kafka automatically overrides the auto-commit interval to **50 milliseconds** to speed up commit waits when auto-commit is enabled. No explicit configuration is needed. Use <xref:Silverback.Messaging.Configuration.Kafka.IMockedKafkaOptionsBuilder.OverrideAutoCommitIntervalMs(System.Nullable{System.Int32})> only when a test requires different commit timing. Passing `null` disables the override and uses the consumer's configured interval, or 5 seconds if no interval is configured.

### Topic Patterns

The mocked Kafka supports the [pattern subscriptions](xref:kafka-subscription#subscribe-by-pattern), matching them as .NET regular expressions against the in-memory topics (requiring the pattern to match the entire topic name with the consumer group protocol, like the broker), and rebalances as soon as a matching topic is created, without waiting for a metadata refresh. Without committed offsets, the mocked consumers always start from the beginning of the partitions, regardless of the offset reset setting.

`WaitUntilAllMessagesAreConsumedAsync` and `WaitUntilAllMessagesAreCommittedAsync` accept the name of an endpoint subscribing patterns, as well as a single topic matching them. The waits apply to whole topics: waiting for an endpoint subscribing a pattern also waits for the matching topics routed to other endpoints.

`GetTopic` creates the topic if it doesn't exist yet, triggering the rebalance of the consumers subscribing a matching pattern. Specify the bootstrap servers if no topic exists yet, as with consumers subscribing only patterns.

### Access In-Memory Topics

When using the mocked Kafka broker you can inspect the internal topics via <xref:Silverback.Testing.IKafkaTestingHelper.GetTopic(System.String,System.String)>.

```csharp
var topic = Helper.GetTopic("test-topic");

// For example: inspect partitions/messages (API depends on the topic implementation)
// topic.Partitions...
```

If your test configures multiple Kafka clusters, use the overload that also takes the `bootstrapServers`.

### Inspect Consumer Groups

The mocked broker tracks consumer groups and offsets. You can access them via <xref:Silverback.Testing.IKafkaTestingHelper.ConsumerGroups> or retrieve a specific group via <xref:Silverback.Testing.IKafkaTestingHelper.GetConsumerGroup(System.String)>.

```csharp
var group = Helper.GetConsumerGroup("test-group");
// group.Consumers / group.CommittedOffsets ...
```

### Use the Admin Client

The <xref:Silverback.Messaging.Broker.Kafka.IConfluentAdminClientFactory> returns a mocked admin client (see <xref:kafka-admin-client>) that works on the in-memory topics. It supports `GetMetadata` and `CreateTopicsAsync` (without the `ValidateOnly` option), while the other methods throw a `NotSupportedException`.

`CreateTopicsAsync` throws a `CreateTopicsException` if the topic already exists. The in-memory topics are created on the fly, so a topic might already exist with the default number of partitions if it has been used before.

### Create a Producer On-the-Fly

Sometimes you want to produce to Kafka without going through the application’s publisher (e.g., to simulate an external producer).

```csharp
IProducer producer = Helper.GetProducer(
    producer => producer
        .WithBootstrapServers("PLAINTEXT://tests")
        .Produce<SomeMessage>(endpoint => endpoint.ProduceTo("test-topic")));

await producer.ProduceAsync(new SomeMessage());
```

## MQTT Specifics

### Inspect Client Sessions

With the mocked MQTT broker you can inspect client sessions via <xref:Silverback.Testing.IMqttTestingHelper.GetClientSession(System.String)>.

```csharp
var session = Helper.GetClientSession("client-id");
// inspect subscriptions, pending messages, ...
```

### Read Published Messages

To quickly retrieve the raw MQTT messages published to a topic, use <xref:Silverback.Testing.IMqttTestingHelper.GetMessages(System.String)>.

```csharp
var messages = Helper.GetMessages("test/topic");
messages.Count.ShouldBeGreaterThan(0);
```

### Create a Producer On-the-Fly

```csharp
IProducer producer = Helper.GetProducer(
    client => client
        .ConnectViaTcp("tests")
        .WithClientId("external-producer")
        .Produce<SomeMessage>(endpoint => endpoint.ProduceTo("test/topic")));

await producer.ProduceAsync(new SomeMessage());
```

## Envelope Builders

Builders are also available to create the <xref:Silverback.Messaging.Messages.IInboundEnvelope`1> and <xref:Silverback.Messaging.Messages.IOutboundEnvelope`1> for the unit tests:
* <xref:Silverback.Messaging.Messages.InboundEnvelopeBuilder`1>
* <xref:Silverback.Messaging.Messages.OutboundEnvelopeBuilder`1>

```csharp
var envelope = new InboundEnvelopeBuilder<MyMessage>()
    .WithMessage(new MyMessage() { ... })
    .WithKafkaKey("1234")
    .WithKafkaTopic("test-topic")
    .Build();
```
