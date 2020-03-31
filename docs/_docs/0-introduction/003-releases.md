---
title:  Releases
permalink: /docs/releases
toc: true
---

## [2.1.0-rc1](https://github.com/BEagle1984/silverback/releases/tag/2.1.0-rc1)

### What's new
* **[Rabbit]** Added consumer `PrefetchSize` and `PrefetchCount` settings (see [Endpoint]({{ site.baseurl }}/docs/configuration/endpoint))
* **[Rabbit]** Added `AcknowledgeEach` to the `RabbitConsumerEndpoint` to define the number of message processed before sending the acknowledgment to the server (see [Endpoint]({{ site.baseurl }}/docs/configuration/endpoint))
* **[Kafka]** The `KafkaStasticsEvent` JSON is now being deserialized and provided as object (in addition to the raw JSON)
* **[Kafka]** Added support for [Apache Avro](https://avro.apache.org/) and schema registry (see [Serialization]({{ site.baseurl }}/docs/advanced/serialization))
* **[Integration]** Multiple message brokers (Kafka and RabbitMQ) can be used together in the same application (see [Connecting to a Message Broker]({{ site.baseurl }}/docs/quickstart/message-broker))

### Breaking Changes
* **[Integration]** `WithConnectionTo<>`, `WithConnectionToKafka` and `WithConnectionToRabbitMQ` are deprecated (but they will still be supported in this version), please use the new `WithConnectionToMessageBroker` and `AddKafka`/`AddRabbit` methods (see [Connecting to a Message Broker]({{ site.baseurl }}/docs/quickstart/message-broker))
* **[Integration]** The `IBroker` inteface and `Broker` abstract base class have been modified to explicitly declare which endpoint type is being handled by the broker implementation (this shouldn't affect you, unless you built your own `IBroker` implementation)
* **[Integration]** The `IMessageSerializer` interfaces has been changed (this shouldn't affect you, unless you built a custom serializer)

## [2.0.0](https://github.com/BEagle1984/silverback/releases/tag/2.0.0)

### What's new
* Created `Silverback.Integration.RabbitMQ` package to connect Silverback with RabbitMQ (see [Connecting to a Message Broker]({{ site.baseurl }}/docs/quickstart/message-broker))
* Messages with an empty body can now be subscribed (you must subscribe to `IInboundEnvelope`) [[#61](https://github.com/BEagle1984/silverback/issues/61)]
* The Kafka partition start offset can now be manually set when a partition is assigned to the consumer (see [Kafka Events]({{ site.baseurl }}/docs/kafka/events)) [[#57](https://github.com/BEagle1984/silverback/issues/57)]
* Full support for multiple consumer groups running in the same process (see [Multiple Consumer Groups (in same process)]({{ site.baseurl }}/docs/kafka/multiple-consumer-groups)) [[#59](https://github.com/BEagle1984/silverback/issues/59)]
* A `KafkaStatisticsEvents` is published also by the `KafkaPRoducer` (previously done in `KafkaConsumer` only)
* Several reliability and performance related improvements

### Breaking Changes
* The `IBroker`, `IProducer` and `IConsumer` interfaces have been slightly modified (it shouldn't affect you unless you built your own `IBroker` implementation)
* Many interfaces (such as `IBehavior`) and delegates have been sligthly modified to pass around an `IReadOnlyCollection<T>` instead of an `IEnumerable<T>`, to avoid the possible issues related to multiple enumeration of an `IEnumerable`
* The `IMessageKeyProvider` interface has been renamed to `IMessageIdProvider` to prevent to be mistaken with the Kafka Key or Rabbit's Routing Key
* `IInboundMessage`/`IOutboundMessage` (plus all the related types) have been renamed to `IInboundEnvelope`/`IOutboundEnvelope` and the property containing the actual message has been renamed from `Content` to `Message`
* The `MustUnwrap` option has been removed from the inbound connector configuration (messages are unwrapped by default)

## [1.2.0](https://github.com/BEagle1984/silverback/releases/tag/1.2.0)

### What's new
* Some new events are published to the internal bus as a consequence to the Kafka events such as partitions assigned or revoked (see [Kafka Events]({{ site.baseurl }}/docs/kafka/events)) [[#34](https://github.com/BEagle1984/silverback/issues/34)]

## [1.1.0](https://github.com/BEagle1984/silverback/releases/tag/1.1.0)

### What's new
* Added `IEndpointsConfigurator` interface to allow splitting the endpoints configuration across multiple types (see [Connecting to a Message Broker]({{ site.baseurl }}/docs/quickstart/message-broker#using-iendpointsconfigurator))
* Added support for distributed tracing (based on [System.Diagnostics](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity?view=netcore-3.1))
* Added `IProducerBehavior` and `IConsumerBehavior` to create an extension point closer to the actual message broker logic (see [Behaviors]({{ site.baseurl }}/docs/quickstart/behaviors))

### Breaking Changes
* `ISortedBehavior` was removed and replaced by a generic `ISorted` interface

## [1.0.5](https://github.com/BEagle1984/silverback/releases/tag/1.0.5)

### What's new
* Upgrade to [Confluent.Kafka 1.3.0](https://github.com/confluentinc/confluent-kafka-dotnet/releases/tag/v1.3.0)

### Fixes
* Fixed `OutboundQueueHealthCheck` [[#43](https://github.com/BEagle1984/silverback/issues/43)]
* The `KafkaProducer` is not disposed by default anymore when a `KafkaException` in thrown (creating too many instances of the producer over a short time span could lead to too many active TCP connections)
* Fixed the bug preventing a `KafkaConsumerEndpoint` pointing to multiple topics to be successfully subscribed

## [1.0.4](https://github.com/BEagle1984/silverback/releases/tag/1.0.4)

### Fixes
* It is finally safe to consume and produce the same type of messages from within the same process (in a natural way, without any extra configuration)
    * Since version [1.0.0](#100) the messages routed to an endpoint aren't forwarded to any subscriber directly
    * Now the inbound connector has been fixed as well, preventing the inbound messages to be immediately routed once again to the outbound endpoint and eliminating all possible causes of mortal loops

## [1.0.3](https://github.com/BEagle1984/silverback/releases/tag/1.0.3)

### Fixes
* Kafka message key is not hashed anymore to avoid possible collisions and simplify debugging
* Not really a fix but `PartitioningKeyMemberAttribute` has been deprecated in favor of `KafkaKeyMemberAttribute`, since the message key isn't used just for partitioning (see [Kafka Message Key]({{ site.baseurl }}/docs/kafka/message-key))

## [1.0.2](https://github.com/BEagle1984/silverback/releases/tag/1.0.2)

### Fixes
* Reintroduced `Add*Subscriber` and `Add*Behavior` as `IServiceCollection` extension methods (for backward compatibility and greater flexibility) [[#41](https://github.com/BEagle1984/silverback/issues/41)]
* Added `WithInMemoryBroker` and `OverrideWithInMemoryBroker` extension methods (see [Testing]({{ site.baseurl }}/docs/quickstart/testing))


## [1.0.0](https://github.com/BEagle1984/silverback/releases/tag/1.0.0)

### What's new
* Message size optimization (no wrappers anymore)
* Better headers usage: identifiers, types, chunks information, etc. are now all sent in the headers
* Reviewed severity of some log entries
* Cleaner internal implementation
* Better exception handling (flattening of `AggregateException`)
* Upgrade to [Confluent.Kafka 1.2.2](https://github.com/confluentinc/confluent-kafka-dotnet/releases/tag/v1.2.2)
* The Kafka consumer automatically recovers from fatal errors (can be disabled via Endpoint configuration)
* Support for .Net Core 3.0 and Entity Framework Core 3.0
* Refactored packages (EF binding logic is now in a single package, versioned after the related EF version)
* Better and cleaner configuration API (see for example [Using the Bus]({{ site.baseurl }}/docs/quickstart/bus) and [Behaviors]({{ site.baseurl }}/docs/quickstart/behaviors))
* Some performance improvements and optimizations (including [#37](https://github.com/BEagle1984/silverback/issues/37))
* Improved database locks mechanism (used also to run the `OutboundQueueWorker`)

### Fixes
* Fixed issue requiring types not implementing `IMessage` to be registered with `HandleMessagesOfType<T>` to consume them [[#33](https://github.com/BEagle1984/silverback/issues/33)]
* Mitigated issue causing the `DistributedBackgroundService` to sometime fail to acquire the database lock [[#39](https://github.com/BEagle1984/silverback/issues/39)]
* Fixed partition key value being lost when using the `DeferredOutboundConnector`
* Other small fixes to improve stability and reliability

### Breaking Changes
* By default the messages published via `IPublisher` that are routed to an outbound endpoint are not sent through to the internal bus and cannot therfore be subscribed locally, within the same process (see [Outbound Connector]({{ site.baseurl }}/docs/configuration/outbound))
* Some changes in `IInboundMessage` and `IOutboundMessage` interfaces
* Changes to the schema of the outbox table (`Silverback.Messaging.Connectors.Model.OutboundMessage`)
* The configuration fluent API changed quite a bit, refer to the current documentation (e.g. [Using the Bus]({{ site.baseurl }}/docs/quickstart/bus) and [Connecting to a Message Broker]({{ site.baseurl }}/docs/quickstart/message-broker))

**Important!** `WithConnectionTo<KafkaBroker>` has to be replaced with `WithConnectionToKafka` in order for all features to work properly. When failing to do so no message key will be generated, causing the messages to land in a random partition and/or preventing to publish to a compacted topic. (see [Kafka Message Key]({{ site.baseurl }}/docs/kafka/message-key))
{: .notice--warning}

* `Silverback.Integration.EntityFrameworkCore` and `Silverback.EventSourcing.EntityFrameworkCore` have been deprecated (`Silverback.Core.EntityFrameworkCore` contains all the necessary logic to use EF as store)
* `KeyMemberAttribute` has been renamed to `PartitioningKeyMemberAttribute` (see [Kafka Message Key]({{ site.baseurl }}/docs/kafka/message-key))

## [0.10.0](https://github.com/BEagle1984/silverback/releases/tag/0.10.0)

### What's new
* Better error handling: now all exceptions, including the ones thrown by the `MessageSerializer` can be handled through the error policies
* Improved logs: promoted some important logs to Information level, writing all processing errors as (at least) Warning and improved logged information quality (logged attributes)
* Add ability to modify messages and headers when moving them via `MoveMessageErrorPolicy`
* Message processing refactoring leading to cleaner, more extensible and predictable API and behavior

### Fixes
* Several other small (and not so small) issues and bugs

## 0.8.0 - 0.9.0

Released two versions mostly to fix bugs, do some small adjustments according to some user feedbacks and update the external dependencies (e.g. Confluent.Kafka 1.0.1).

### Fixes
* Fixed exception loading error policies from json in Silverback.Integration.Configuration [[#24](https://github.com/BEagle1984/silverback/issues/24)]

## 0.7.0

### What's new
* [Confluent.Kafka 1.0.0](https://github.com/confluentinc/confluent-kafka-dotnet/releases/tag/v1.0.0) has finally been released and it has been integrated and tested with this version of Silverback
* Created a simple event store that perfectly integrates with the rest of the Silverback framework (see [Event Sourcing]({{ site.baseurl }}/docs/quickstart/event-sourcing))
* Silverback.Integration.InMemory to mock the message broker behavior in your unit tests
* Several small optimizations and improvements

## 0.6.0

### What's new
* Added support for message headers (only accessible from [Behaviors]({{ site.baseurl }}/docs/quickstart/behaviors) or "low-level" [Broker]({{ site.baseurl }}/docs/advanced/broker) implementation)
* Simplified message subscription even further: now all public methods of the types implementing the marker interface `ISubscriber` are automatically subscribed by default without having to annotate them with the `SubscribeAttribute` (this behavior is customizable)
* Upgrade to [Confluent.Kafka 1.0.0-RC1](https://github.com/confluentinc/confluent-kafka-dotnet/releases/tag/v1.0-RC1)

## 0.3.x - 0.5.x

Some releases where done adding quite a few features-

### What's new
* Silverback.Integration.Configuration package to load the inbound/outbound configuration from the app.settings json
* Batch processing
* Parallel subscribers
* Delegate subscription as an alternative to `SubscribeAttribute` based subscription
* Improved support for Rx.net
* Support for legacy messages and POCO classes
* Offset storage as an alternative and more optimized way to guarantee exactly once processing, storing just the offset of the last message instead of logging every message  (see [Inbound Connector]({{ site.baseurl }}/docs/configuration/inbound))
* Behaviors as a convenient way to implement your cross-cutting concerns (like logging, validation, etc.) to be plugged into the internal bus publishing pipeline (see [Behaviors]({{ site.baseurl }}/docs/quickstart/behaviors))
* Message chunking to automatically split the larger messages and rebuild them on the other end (see [Chunking]({{ site.baseurl }}/docs/advanced/chunking))
* much more...a huge amount of refactorings

### Fixes
* Several fixes and optimizations

## 0.3.2

The very first public release of Silverback! It included:
* In-process message bus
* Inbound/outbound connector for message broker abstraction
* Kafka broker implementation
* Outbox table pattern implementation
* Exactly once processing
* ...
