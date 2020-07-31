---
uid: releases
---

# Releases

## [3.0.0-beta.4](https://github.com/BEagle1984/silverback/releases/tag/v3.0.0-beta.4)

### What's new

* Simplify subscribers registration and get rid of the `ISubscriber` interface (see <xref:subscribe>)
* Simplify configuration and reduce boiler plate (see <xref:subscribe> and <xref:message-broker>)
* Connect brokers and handle graceful shutdown automatically (see <xref:message-broker>)
* Scan subscribers automatically at startup to reduce cost of first message
* Add `IServiceCollection.ConfigureSilverback` extension method to conveniently split the configuration code (see <xref:enabling-silverback>)
* Improve code quality (enhance CI pipeline to use Roslyn analyzers and integrate [SonarCloud](https://sonarcloud.io/dashboard?id=silverback))
* Enable nullable reference types and adjust all API
* Document the entire public API (see [API Documentation](~/api/Microsoft.Extensions.DependencyInjection.html))
* Add option to throw an exception if no subscriber is handling a message that was published to the internal bus or was consumed from a message broker (see `throwIfUnhandled` argument in the [`IPublisher`](xref:Silverback.Messaging.Publishing.IPublisher) methods and [`ThrowIfUnhandled`](xref:Silverback.Messaging.IConsumerEndpoint#Silverback_Messaging_IConsumerEndpoint_ThrowIfUnhandled) property in the [`IConsumerEndpoint`](xref:Silverback.Messaging.IConsumerEndpoint))
* Replace Newtonsoft.Json with System.Text.Json to improve serialization and deserialization performance (the old serializers have been moved into the Silverback.Integration.Newtonsoft package, see <xref:serialization>d)
* Upgrade to [Confluent.Kafka 1.4.4](https://github.com/confluentinc/confluent-kafka-dotnet/releases/tag/v1.4.4)
* Upgrade to [RabbitMQ.Client 6.1.0](https://github.com/rabbitmq/rabbitmq-dotnet-client/releases/tag/v6.1.0)
* Add log levels configuration (see <xref:logging>)
* Add consumer status information and statistics (see <xref:message-broker#consumer-management-api>)
* Add basic consumers health check (see <xref:message-broker#health-monitoring>)

### Fixes

* Fix `DeferredOutboundConnector` not publishing custom headers [[#102](https://github.com/BEagle1984/silverback/issues/102)]

### Breaking Changes

* Removed `ISubscriber` interface
* Removed `BusConfigurator` (moved all the configuration into `IServiceCollection` and `ISilverbackBuilder` extension methods)
    * Replaced `BusConfigurator.Connect` with `IBrokerOptionsBuilder.AddInboundEnpoint`, `IBrokerOptionsBuilder.AddOutboundEndpoint` and `IEndpointConfigurator` (see <xref:message-broker>)
    * Replaced `BusConfigurator.Subscribe` methods with `ISilverbackBuilder.AddDelegateSubscriber` or `IServiceCollection.AddDelegateSubscriber` (see <xref:subscribe>)
    * Replaced `BusConfigurator.HandleMessagesOfType` methods with `ISilverbackBuilder.HandleMessagesOfType` (see <xref:subscribe>)
    * `BusConfigurator.ScanSubscribers` is not needed anymore since it gets called automatically at startup (from an `IHostedService`)
* Removed `IServiceCollection.Add*Subscriber`, `IServiceCollection.Add*Behavior`, `IServiceCollection.Add*BrokerBehavior`, `IServiceCollection.Add*EndpointsConfigurator`, `IServiceCollection.Add*OutboundRouter` extension methods, use `IServiceCollection.ConfigureSilverback` instead (see <xref:enabling-silverback>)
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
    * Modified schema of `TemporaryMessageChunk`, `StoredOffset` and `OutboundMessage` entities
* Moved and renamed some internally used types (e.g. `QueuedMessage`, `DbQueuedMessage`, ...)
* Some changes to error policies:
    * `Apply` method is now async
    * Changed the signature of the transfor function in the `MovePolicy`
* Removed `IMessageIdProvider` and all related logic: **the `Id` or `MessageId` property will not be automatically initialized anymore and its value will not be used as identifier for the outbound message anymore (refer to the <xref:message-id> page for further details on how to set a custom message id, if needed)
* `WithConnectionTo<>`, `WithConnectionToKafka`, `WithConnectionToRabbitMQ` and `WithInMemoryBroker` have been removed, please use the new `WithConnectionToMessageBroker` and `AddKafka`/`AddRabbit`/`AddInMemoryBroker` methods (see <xref:message-broker>)
* Some minor breaking changes to the `InMemoryBroker`
* Removed `PartitioningKeyMemberAttribute`, use `KafkaKeyMemberAttribute` instead
* `Silverback.Integration.Configuration` has been discontinued
* The `Settings` property has been renamed to `Options` in the default `JsonMessageSerializer` (since the switch to System.Text.Json)
* Removed `LogWithLevel` method from `SkipMessageErrorPolicy`, use the new `WithLogLevels` configuration instead

## [2.2.0](https://github.com/BEagle1984/silverback/releases/tag/v2.2.0)

### What's new
* Allow custom outbound routers to be registered as scoped or transient (instead of singleton only)

## [2.1.2](https://github.com/BEagle1984/silverback/releases/tag/v2.1.2)

### Fixes
* Fix delay in Retry policy [[#97](https://github.com/BEagle1984/silverback/issues/97)]

## [2.1.1](https://github.com/BEagle1984/silverback/releases/tag/v2.1.1)

### What's new
* Add support for multiple message brokers (Kafka and RabbitMQ) in the same application (see <xref:message-broker>)
* Add end-to-end message encryption (see <xref:encryption>)
* Add dynamic custom routing of outbound messages (see <xref:outbound>)
* Improve support for message headers (see <xref:headers>)
* Add support for binary files (see <xref:binary-files>)
* Improve message identifier handling: the `IIntegrationMessage` is not required to have an `Id` property anymore (the `x-message-id` header will still be generated and if the property exists will continue to be automatically initialized)
* `x-first-chunk-offset` header added by default (see <xref:headers>)
* Deserialize `KafkaStasticsEvent` JSON and provided its content as an object (in addition to the raw JSON)
* Add support for [Apache Avro](https://avro.apache.org/) and schema registry (see <xref:serialization>)
* Upgrade to [Confluent.Kafka 1.4.2](https://github.com/confluentinc/confluent-kafka-dotnet/releases/tag/v1.4.2)
* Add consumer `PrefetchSize` and `PrefetchCount` settings (see <xref:endpoint>))
* Add `AcknowledgeEach` to the `RabbitConsumerEndpoint` to define the number of message processed before sending the acknowledgment to the server (see <xref:endpoint>))
* Upgrade to [RabbitMQ.Client 6.0.0](https://github.com/rabbitmq/rabbitmq-dotnet-client/releases/tag/v6.0.0)
* Improve message type resolution performance and reliability in `JsonMessageSerializer`
* Add `LogWithLevel` method to `SkipMessageErrorPolicy` to specify the desired level for the "Message skipped" log entry (the default is now increased to `Error`)

### Breaking Changes
These changes shouldn't affect you unless you built your own `IBroker` implementation or are interacting at low-level with the `IBroker` (this is why has been decided to still mark this as a minor release):
* The `IBroker` inteface and `Broker` abstract base class have been modified to explicitly declare which endpoint type is being handled by the broker implementation
* The `IMessageSerializer` interfaces has been changed
* The `IConsumerBehavior` and `IProducerBehavior` interfaces have been changed and moved into `Integration.Broker.Behaviors` namespace
* Changed the parameters order in some less used overloads in the `IBrokerOptionBuilder`

### Announced Breaking Changes
These aren't real breaking changes but some methods have been marked as deprecated and will be removed in one of the next major releases:
* `WithConnectionTo<>`, `WithConnectionToKafka` and `WithConnectionToRabbitMQ` are deprecated (they will still be supported in this version), please use the new `WithConnectionToMessageBroker` and `AddKafka`/`AddRabbit` methods (see <xref:message-broker>)

## [2.0.0](https://github.com/BEagle1984/silverback/releases/tag/v2.0.0)

### What's new
* Create `Silverback.Integration.RabbitMQ` package to connect Silverback with RabbitMQ (see <xref:message-broker>)
* Enable subscription of messages with an empty body (you must subscribe to the `IInboundEnvelope`) [[#61](https://github.com/BEagle1984/silverback/issues/61)]
* Add hook to manually set the Kafka partition start offset when a partition is assigned to the consumer (see <xref:kafka-events>) [[#57](https://github.com/BEagle1984/silverback/issues/57)]
* Support for multiple consumer groups running in the same process (see <xref:kafka-consumer-groups>) [[#59](https://github.com/BEagle1984/silverback/issues/59)]
* Publish `KafkaStatisticsEvents` also from the `KafkaProducer` (previously done in `KafkaConsumer` only)
* Several reliability and performance related improvements

### Breaking Changes
* The `IBroker`, `IProducer` and `IConsumer` interfaces have been slightly modified (it shouldn't affect you unless you built your own `IBroker` implementation)
* Many interfaces (such as `IBehavior`) and delegates have been sligthly modified to pass around an `IReadOnlyCollection<T>` instead of an `IEnumerable<T>`, to avoid the possible issues related to multiple enumeration of an `IEnumerable`
* The `IMessageKeyProvider` interface has been renamed to `IMessageIdProvider` to prevent to be mistaken with the Kafka Key or Rabbit's Routing Key
* `IInboundMessage`/`IOutboundMessage` (plus all the related types) have been renamed to `IInboundEnvelope`/`IOutboundEnvelope` and the property containing the actual message has been renamed from `Content` to `Message`
* The `MustUnwrap` option has been removed from the inbound connector configuration (messages are unwrapped by default)

## [1.2.0](https://github.com/BEagle1984/silverback/releases/tag/v1.2.0)

### What's new
* Publish events to the internal bus as a consequence to the Kafka events such as partitions assigned or revoked (see <xref:kafka-events>) [[#34](https://github.com/BEagle1984/silverback/issues/34)]

## [1.1.0](https://github.com/BEagle1984/silverback/releases/tag/v1.1.0)

### What's new
* Add `IEndpointsConfigurator` interface to allow splitting the endpoints configuration across multiple types (see <xref:message-broker>)
* Add support for distributed tracing (based on [System.Diagnostics](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity?view=netcore-3.1))
* Add `IProducerBehavior` and `IConsumerBehavior` to create an extension point closer to the actual message broker logic (see <xref:behaviors>)

### Breaking Changes
* `ISortedBehavior` was removed and replaced by a generic `ISorted` interface

## [1.0.5](https://github.com/BEagle1984/silverback/releases/tag/v1.0.5)

### What's new
* Upgrade to [Confluent.Kafka 1.3.0](https://github.com/confluentinc/confluent-kafka-dotnet/releases/tag/v1.3.0)

### Fixes
* Fix `OutboundQueueHealthCheck` [[#43](https://github.com/BEagle1984/silverback/issues/43)]
* Remove automatic disposal of the `KafkaProducer` when a `KafkaException` is thrown (creating too many instances of the producer over a short time span could lead to too many active TCP connections)
* Fix the bug preventing a `KafkaConsumerEndpoint` pointing to multiple topics to be successfully subscribed

## [1.0.4](https://github.com/BEagle1984/silverback/releases/tag/v1.0.4)

### Fixes
* Fix mortal loop issue: it is finally safe to consume and produce the same type of messages from within the same process (in a natural way, without any extra configuration)
    * Since version [1.0.0](#100) the messages routed to an endpoint aren't forwarded to any subscriber directly
    * Now the inbound connector has been fixed as well, preventing the inbound messages to be immediately routed once again to the outbound endpoint and eliminating all possible causes of mortal loops

## [1.0.3](https://github.com/BEagle1984/silverback/releases/tag/v1.0.3)

### What's new
* Deprecate `PartitioningKeyMemberAttribute` in favor of `KafkaKeyMemberAttribute`, since the message key isn't used just for partitioning (see <xref:kafka-message-key>)

### Fixes
* Forward Kafka message key as-is (not hashed anymore) to avoid possible collisions and simplify debugging

## [1.0.2](https://github.com/BEagle1984/silverback/releases/tag/v1.0.2)

### Fixes
* Reintroduce `Add*Subscriber` and `Add*Behavior` as `IServiceCollection` extension methods (for backward compatibility and greater flexibility) [[#41](https://github.com/BEagle1984/silverback/issues/41)]
* Add `WithInMemoryBroker` and `OverrideWithInMemoryBroker` extension methods (see <xref:testing>)


## [1.0.0](https://github.com/BEagle1984/silverback/releases/tag/v1.0.0)

### What's new
* Optimize message size (no wrappers anymore)
* Improve headers usage: identifiers, types, chunks information, etc. are now all sent in the headers
* Review severity of some log entries
* Improve and clean up internal implementation
* Improve exception handling (flattening of `AggregateException`)
* Upgrade to [Confluent.Kafka 1.2.2](https://github.com/confluentinc/confluent-kafka-dotnet/releases/tag/v1.2.2)
* Add automatic recovers from fatal errors in `KafkaConsumer` (can be disabled via Endpoint configuration)
* Support .Net Core 3.0 and Entity Framework Core 3.0
* Refactor packages (EF binding logic is now in a single package, versioned after the related EF version)
* Improve configuration API (see for example [Using the Bus](quickstart/bus) and <xref:behaviors>)
* Improve and optimize performance (including [#37](https://github.com/BEagle1984/silverback/issues/37))
* Improve database locks mechanism (used also to run the `OutboundQueueWorker`)

### Fixes
* Fixe issue requiring types not implementing `IMessage` to be registered with `HandleMessagesOfType<T>` to consume them [[#33](https://github.com/BEagle1984/silverback/issues/33)]
* Mitigate issue causing the `DistributedBackgroundService` to sometime fail to acquire the database lock [[#39](https://github.com/BEagle1984/silverback/issues/39)]
* Fix partition key value being lost when using the `DeferredOutboundConnector`
* Other small fixes to improve stability and reliability

### Breaking Changes
* By default the messages published via `IPublisher` that are routed to an outbound endpoint are not sent through to the internal bus and cannot therfore be subscribed locally, within the same process (see <xref:outbound>)
* Some changes in `IInboundMessage` and `IOutboundMessage` interfaces
* Changes to the schema of the outbox table (`Silverback.Messaging.Connectors.Model.OutboundMessage`)
* The configuration fluent API changed quite a bit, refer to the current documentation (e.g. [Using the Bus](quickstart/bus) and <xref:message-broker>)

> [!Important]
> `WithConnectionTo<KafkaBroker>` has to be replaced with `WithConnectionToKafka` in order for all features to work properly. When failing to do so no message key will be generated, causing the messages to land in a random partition and/or preventing to publish to a compacted topic. (see <xref:kafka-message-key>)

* `Silverback.Integration.EntityFrameworkCore` and `Silverback.EventSourcing.EntityFrameworkCore` have been deprecated (`Silverback.Core.EntityFrameworkCore` contains all the necessary logic to use EF as store)
* `KeyMemberAttribute` has been renamed to `PartitioningKeyMemberAttribute` (see <xref:message-broker>)

## [0.10.0](https://github.com/BEagle1984/silverback/releases/tag/v0.10.0)

### What's new
* Improve error handling: now all exceptions, including the ones thrown by the `MessageSerializer` can be handled through the error policies
* Improve logs: promoted some important logs to Information level, writing all processing errors as (at least) Warning and improved logged information quality (logged attributes)
* Add ability to modify messages and headers when moving them via `MoveMessageErrorPolicy`
* Refactor message processing to a cleaner, more extensible and predictable API and behavior

### Fixes
* Fixed several small (and not so small) issues and bugs

## 0.8.0 - 0.9.0

Released two versions mostly to fix bugs, do some small adjustments according to some user feedbacks and update the external dependencies (e.g. Confluent.Kafka 1.0.1).

### Fixes
* Fix exception loading error policies from JSON in Silverback.Integration.Configuration [[#24](https://github.com/BEagle1984/silverback/issues/24)]

## 0.7.0

### What's new
* Upgrade to [Confluent.Kafka 1.0.0](https://github.com/confluentinc/confluent-kafka-dotnet/releases/tag/v1.0.0)
* Create a simple event store that perfectly integrates with the rest of the Silverback framework (see <xref:event-sourcing>)
* Add Silverback.Integration.InMemory package to mock the message broker behavior in your unit tests
* Several small optimizations and improvements

## 0.6.0

### What's new
* Add support for message headers (see <xref:behaviors> and <xref:ibroker>)
* Simplify message subscription even further: now all public methods of the types implementing the marker interface `ISubscriber` are automatically subscribed by default without having to annotate them with the `SubscribeAttribute` (this behavior is customizable)
* Upgrade to [Confluent.Kafka 1.0.0-RC1](https://github.com/confluentinc/confluent-kafka-dotnet/releases/tag/v1.0-RC1)

## 0.3.x - 0.5.x

Some releases where done adding quite a few features.

### What's new
* Add Silverback.Integration.Configuration package to load the inbound/outbound configuration from the app.settings json
* Add batch processing
* Add parallel subscribers
* Add delegate subscription as an alternative to `SubscribeAttribute` based subscription
* Improve support for Rx.net
* Add support for legacy messages and POCO classes
* Add offset storage as an alternative and more optimized way to guarantee exactly once processing, storing just the offset of the last message instead of logging every message (see <xref:inbound>)
* Add behaviors as a convenient way to implement your cross-cutting concerns (like logging, validation, etc.) to be plugged into the internal bus publishing pipeline (see <xref:behaviors>)
* Add message chunking to automatically split the larger messages and rebuild them on the other end (see <xref:chunking>)
* ...much more...and a huge amount of refactorings

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
