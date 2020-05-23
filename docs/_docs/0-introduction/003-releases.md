---
title:  Releases
permalink: /docs/releases
toc: true
---

## [3.0.0-beta1](https://github.com/BEagle1984/silverback/releases/tag/v3.0.0-beta1)

### What's new

* Improved code quality (xml documentation, pipeline, analyzers, ...)
* Decorated nullable reference types
* Improved performance (show benchmarks)

### Fixes

### Breaking Changes

* `BusConfigurator` replaced with `IBusConfigurator` interface
* `ErrorPolicyBuilder` replaced with `IErrorPolicyBuilder` interface
* The visibility of some types has been changed to internal to favor a cleaner and clearer API where the public types are well documented and their backward compatibility is valued
* Removed `Silverback` prefix from exceptions name
* Removed the `IRequest<TResponse>` interface (it was implemented by both `IQuery<TResult>` and `ICommand<TResult>`)
* Changed _Impl_ methods suffix with _Core_, this affects some virtual members in the `Broker` and other base classes
* `IConsumer.Received` event replaced by a callback delegate
* `IBroker.GetConsumer` and `IBrokerCollection.GetConsumer` methods renamed to `AddConsumer`
* `IQueueProduer` and `IQueueConsumer` renamed to `IQueueWriter` and `IQueueReader`
* Messages with a `null` or `empty` body can be subscribed as `IInboundEnvelope<TMessage>` as well, as long as the `x-message-type` header is set or a typed serializer such as `JsonMessageSerializer<TMessage>` is used
* Database:
    * Moved all entities (used with Entity Framework Core) to the `Silverback.Database.Model` namespace
    * Replaced `InboundMessage` entity with `InboundLogEntry`
    * Changed some fields in `TemporaryMessageChunk`
* Moved and renamed some internally used types (e.g. `QueuedMessage`, `DbQueuedMessage`, ...)
* Some changes to error policies:
    * `Apply` method is now async
    * Changed the signature of the transfor function in the `MovePolicy`
* Removed `IMessageIdProvider` and all related logic: **the `Id` or `MessageId` property will not be automatically initialized anymore and its value will not be used as identifier for the outbound message anymore (refer to the [Message Identifier]({{ site.baseurl }}/docs/advanced/message-id) page for further details on how to set a custom message id, if needed)
* `WithConnectionTo<>`, `WithConnectionToKafka`, `WithConnectionToRabbitMQ` and `WithInMemoryBroker` have been removed, please use the new `WithConnectionToMessageBroker` and `AddKafka`/`AddRabbit`/`AddInMemoryBroker` methods (see [Connecting to a Message Broker]({{ site.baseurl }}/docs/quickstart/message-broker))
* Some minor breaking changes to the `InMemoryBroker`
* Removed `PartitioningKeyMemberAttribute`, use `KafkaKeyMemberAttribute` instead

## [2.1.1](https://github.com/BEagle1984/silverback/releases/tag/v2.1.1)

### What's new
* Multiple message brokers (Kafka and RabbitMQ) can be used together in the same application (see [Connecting to a Message Broker]({{ site.baseurl }}/docs/quickstart/message-broker))
* End-to-End message encryption (see [Encryption]({{ site.baseurl }}/docs/advanced/encryption))
* Dynamic custom routing of outbound messages (see [Outbound Connector]({{ site.baseurl }}/docs/configuration/outbound))
* Better support for message headers (see [Message Headers]({{ site.baseurl }}/docs/quickstart/headers))
* Binary files support (see [Binary Files]({{ site.baseurl }}/docs/advanced/binary-files))
* The `IIntegrationMessage` is not required to have an `Id` property anymore (the `x-message-id` header will still be generated and if the property exists will continue to be automatically initialized)
* `x-first-chunk-offset` header added by default (see [Message Headers]({{ site.baseurl }}/docs/quickstart/headers))
* <span class="area-kafka" /> The `KafkaStasticsEvent` JSON is now being deserialized and provided as object (in addition to the raw JSON)
* <span class="area-kafka" /> Added support for [Apache Avro](https://avro.apache.org/) and schema registry (see [Serialization]({{ site.baseurl }}/docs/advanced/serialization))
* <span class="area-kafka" /> Upgrade to [Confluent.Kafka 1.4.2](https://github.com/confluentinc/confluent-kafka-dotnet/releases/tag/v1.4.2)
* <span class="area-rabbit" /> Added consumer `PrefetchSize` and `PrefetchCount` settings (see [Endpoint]({{ site.baseurl }}/docs/configuration/endpoint))
* <span class="area-rabbit" /> Added `AcknowledgeEach` to the `RabbitConsumerEndpoint` to define the number of message processed before sending the acknowledgment to the server (see [Endpoint]({{ site.baseurl }}/docs/configuration/endpoint))
* <span class="area-kafka" /> Upgrade to [RabbitMQ.Client 6.0.0](https://github.com/rabbitmq/rabbitmq-dotnet-client/releases/tag/v6.0.0)
* Improved message type resolution performance and reliability in `JsonMessageSerializer`
* `LogWithLevel` method added to `SkipMessageErrorPolicy` to specify the desired level for the "Message skipped" log entry (the default is now increased to `Error`)

### Breaking Changes
These changes shouldn't affect you unless you built your own `IBroker` implementation or are interacting at low-level with the `IBroker` (this is why has been decided to still mark this as a minor release):
* The `IBroker` inteface and `Broker` abstract base class have been modified to explicitly declare which endpoint type is being handled by the broker implementation
* The `IMessageSerializer` interfaces has been changed
* The `IConsumerBehavior` and `IProducerBehavior` interfaces have been changed and moved into `Integration.Broker.Behaviors` namespace
* Changed the parameters order in some less used overloads in the `IBrokerOptionBuilder`

### Announced Breaking Changes
These aren't real breaking changes but some methods have been marked as deprecated and will be removed in one of the next major releases:
* `WithConnectionTo<>`, `WithConnectionToKafka` and `WithConnectionToRabbitMQ` are deprecated (they will still be supported in this version), please use the new `WithConnectionToMessageBroker` and `AddKafka`/`AddRabbit` methods (see [Connecting to a Message Broker]({{ site.baseurl }}/docs/quickstart/message-broker))

## [2.0.0](https://github.com/BEagle1984/silverback/releases/tag/v2.0.0)

### What's new
* Created `Silverback.Integration.RabbitMQ` package to connect Silverback with RabbitMQ (see [Connecting to a Message Broker]({{ site.baseurl }}/docs/quickstart/message-broker))
* Messages with an empty body can now be subscribed (you must subscribe to the `IInboundEnvelope`) [[#61](https://github.com/BEagle1984/silverback/issues/61)]
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

## [1.2.0](https://github.com/BEagle1984/silverback/releases/tag/v1.2.0)

### What's new
* Some new events are published to the internal bus as a consequence to the Kafka events such as partitions assigned or revoked (see [Kafka Events]({{ site.baseurl }}/docs/kafka/events)) [[#34](https://github.com/BEagle1984/silverback/issues/34)]

## [1.1.0](https://github.com/BEagle1984/silverback/releases/tag/v1.1.0)

### What's new
* Added `IEndpointsConfigurator` interface to allow splitting the endpoints configuration across multiple types (see [Connecting to a Message Broker]({{ site.baseurl }}/docs/quickstart/message-broker#using-iendpointsconfigurator))
* Added support for distributed tracing (based on [System.Diagnostics](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity?view=netcore-3.1))
* Added `IProducerBehavior` and `IConsumerBehavior` to create an extension point closer to the actual message broker logic (see [Behaviors]({{ site.baseurl }}/docs/quickstart/behaviors))

### Breaking Changes
* `ISortedBehavior` was removed and replaced by a generic `ISorted` interface

## [1.0.5](https://github.com/BEagle1984/silverback/releases/tag/v1.0.5)

### What's new
* Upgrade to [Confluent.Kafka 1.3.0](https://github.com/confluentinc/confluent-kafka-dotnet/releases/tag/v1.3.0)

### Fixes
* Fixed `OutboundQueueHealthCheck` [[#43](https://github.com/BEagle1984/silverback/issues/43)]
* The `KafkaProducer` is not disposed by default anymore when a `KafkaException` in thrown (creating too many instances of the producer over a short time span could lead to too many active TCP connections)
* Fixed the bug preventing a `KafkaConsumerEndpoint` pointing to multiple topics to be successfully subscribed

## [1.0.4](https://github.com/BEagle1984/silverback/releases/tag/v1.0.4)

### Fixes
* It is finally safe to consume and produce the same type of messages from within the same process (in a natural way, without any extra configuration)
    * Since version [1.0.0](#100) the messages routed to an endpoint aren't forwarded to any subscriber directly
    * Now the inbound connector has been fixed as well, preventing the inbound messages to be immediately routed once again to the outbound endpoint and eliminating all possible causes of mortal loops

## [1.0.3](https://github.com/BEagle1984/silverback/releases/tag/v1.0.3)

### Fixes
* Kafka message key is not hashed anymore to avoid possible collisions and simplify debugging
* Not really a fix but `PartitioningKeyMemberAttribute` has been deprecated in favor of `KafkaKeyMemberAttribute`, since the message key isn't used just for partitioning (see [Kafka Message Key]({{ site.baseurl }}/docs/kafka/message-key))

## [1.0.2](https://github.com/BEagle1984/silverback/releases/tag/v1.0.2)

### Fixes
* Reintroduced `Add*Subscriber` and `Add*Behavior` as `IServiceCollection` extension methods (for backward compatibility and greater flexibility) [[#41](https://github.com/BEagle1984/silverback/issues/41)]
* Added `WithInMemoryBroker` and `OverrideWithInMemoryBroker` extension methods (see [Testing]({{ site.baseurl }}/docs/quickstart/testing))


## [1.0.0](https://github.com/BEagle1984/silverback/releases/tag/v1.0.0)

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

`WithConnectionTo<KafkaBroker>` has to be replaced with `WithConnectionToKafka` in order for all features to work properly. When failing to do so no message key will be generated, causing the messages to land in a random partition and/or preventing to publish to a compacted topic. (see [Kafka Message Key]({{ site.baseurl }}/docs/kafka/message-key))
{: .notice--important}

* `Silverback.Integration.EntityFrameworkCore` and `Silverback.EventSourcing.EntityFrameworkCore` have been deprecated (`Silverback.Core.EntityFrameworkCore` contains all the necessary logic to use EF as store)
* `KeyMemberAttribute` has been renamed to `PartitioningKeyMemberAttribute` (see [Kafka Message Key]({{ site.baseurl }}/docs/kafka/message-key))

## [0.10.0](https://github.com/BEagle1984/silverback/releases/tag/v0.10.0)

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
