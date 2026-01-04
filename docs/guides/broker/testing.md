---
uid: testing
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

> [!TIP]
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
            .AddProducer(
                producer => producer
                    .Produce<SomeMessage>(endpoint => endpoint.ProduceTo("test/topic")))
            .AddConsumer(
                consumer => consumer
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

> [!NOTE]
> `UseMockedKafka` / `UseMockedMqtt` replace the underlying broker connectivity (Confluent.Kafka / MQTTnet) with an in-memory implementation.
> They don’t add producers/consumers/endpoints for you; those still come from your normal app configuration.

## Testing Helpers

The broker-specific testing helper (<xref:Silverback.Testing.IKafkaTestingHelper> or <xref:Silverback.Testing.IMqttTestingHelper>) also implements the base <xref:Silverback.Testing.ITestingHelper> interface.

The most commonly used helpers are:

- <xref:Silverback.Testing.ITestingHelper.WaitUntilConnectedAsync(System.Nullable{System.TimeSpan})> – wait until consumers are connected and ready.
- <xref:Silverback.Testing.ITestingHelper.WaitUntilAllMessagesAreConsumedAsync(System.String[])> – wait until routed messages have been processed and committed (**mocked brokers only**).
- <xref:Silverback.Testing.ITestingHelper.WaitUntilOutboxIsEmptyAsync(System.Nullable{System.TimeSpan})> – wait until the outbox has been drained (if you’re using the outbox).

### Example: publish and wait

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

> [!IMPORTANT]
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

## Kafka Specifics

### Access in-memory topics

When using the mocked Kafka broker you can inspect the internal topics via <xref:Silverback.Testing.IKafkaTestingHelper.GetTopic(System.String,System.String)>.

```csharp
var topic = Helper.GetTopic("test-topic");

// For example: inspect partitions/messages (API depends on the topic implementation)
// topic.Partitions...
```

If your test configures multiple Kafka clusters, use the overload that also takes the `bootstrapServers`.

### Inspect consumer groups

The mocked broker tracks consumer groups and offsets. You can access them via <xref:Silverback.Testing.IKafkaTestingHelper.ConsumerGroups> or retrieve a specific group via <xref:Silverback.Testing.IKafkaTestingHelper.GetConsumerGroup(System.String)>.

```csharp
var group = Helper.GetConsumerGroup("test-group");
// group.Consumers / group.CommittedOffsets ...
```

### Create an ad-hoc producer

Sometimes you want to produce to Kafka without going through the application’s publisher (e.g. to simulate an external producer).

```csharp
var producer = Helper.GetProducer(
    config => config
        .WithBootstrapServers("PLAINTEXT://tests")
        .ProduceTo("test-topic"));

await producer.ProduceAsync(new SomeMessage());
```

## MQTT Specifics

### Inspect client sessions

With the mocked MQTT broker you can inspect client sessions via <xref:Silverback.Testing.IMqttTestingHelper.GetClientSession(System.String)>.

```csharp
var session = Helper.GetClientSession("client-id");
// inspect subscriptions, pending messages, ...
```

### Read published messages

To quickly retrieve the raw MQTT messages published to a topic, use <xref:Silverback.Testing.IMqttTestingHelper.GetMessages(System.String)>.

```csharp
var messages = Helper.GetMessages("test/topic");
messages.Count.ShouldBeGreaterThan(0);
```

### Create an ad-hoc MQTT producer

```csharp
var producer = Helper.GetProducer(
    config => config
        .WithClientId("external-producer")
        .ProduceTo("test/topic"));

await producer.ProduceAsync(new SomeMessage());
```

## Additional Resources
