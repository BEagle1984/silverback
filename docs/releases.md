---
uid: releases
---

# Releases

## [3.5.0](https://github.com/BEagle1984/silverback/releases/tag/v3.5.0)

### What's new

* Log `MqttClient` internal events (see <xref:logging>)
* Upgrade to [Confluent.Kafka 1.8.2](https://github.com/confluentinc/confluent-kafka-dotnet/releases/tag/v1.8.2)
* Upgrade to [MQTTnet 3.0.16](https://github.com/chkr1011/MQTTnet/releases/tag/v3.0.16)
* Upgrade to [RabbitMQ.Client 6.2.2](https://github.com/rabbitmq/rabbitmq-dotnet-client/releases/tag/v6.2.2)
* Update several dependencies

### Fixes

* Fix <xref:Silverback.Messaging.Broker.MqttConsumer> reconnection issues
* Handle edge cases related to MQTT acknowledgment timeout in <xref:Silverback.Messaging.Broker.MqttConsumer>
* Allow max retries specification and error policies chains with MQTT V3

## [3.4.0](https://github.com/BEagle1984/silverback/releases/tag/v3.4.0)

### What's new

* Support encryption key rotation (see <xref:encryption>)

## [3.3.1](https://github.com/BEagle1984/silverback/releases/tag/v3.3.1)

### Fixes

* Fix `AddHeaders<TMessage>` and `WithKafkaKey<TMessage>` not being correctly invoked by all `IProducer.Produce` and `IProducer.ProducerAsync` overloads
* Add endpoint friendly name to all logs

## [3.3.0](https://github.com/BEagle1984/silverback/releases/tag/v3.3.0)

### What's new

* Optimize in-memory mocked Kafka (avoid spawning too many threads)
* Support multiple brokers (with overlapping topic names) in mocked Kafka and MQTT
* Add message validation for both producer and consumer (see <xref:validation>)
* Add new `AddInbound` overloads specifying message type for a more compact configuration when using the typed deserializer (see <xref:serialization>)

### Fixes

* Invoke the Kafka partition EOF callback for all connected consumers
* Ignore null or empty Kafka key in producer

## [3.2.0](https://github.com/BEagle1984/silverback/releases/tag/v3.2.0)

### What's new

* Add new Kafka partition EOF callback to be notified when the end of a partition is reached by the consumer (see <xref:kafka-events> and <xref:Silverback.Messaging.Broker.Callbacks.IKafkaPartitionEofCallback>)
* Allow multiple calls to [IKafkaConsumerEndpointBuilder.Configure](xref:Silverback.Messaging.Configuration.Kafka.IKafkaConsumerEndpointBuilder#Silverback_Messaging_Configuration_Kafka_IKafkaConsumerEndpointBuilder_Configure_System_Action_Silverback_Messaging_Configuration_Kafka_KafkaConsumerConfig__) or [IKafkaProducerEndpointBuilder.Configure](xref:Silverback.Messaging.Configuration.Kafka.IKafkaProducerEndpointBuilder#Silverback_Messaging_Configuration_Kafka_IKafkaProducerEndpointBuilder_Configure_System_Action_Silverback_Messaging_Configuration_Kafka_KafkaProducerConfig__) for the same endpoint
* Observe a grace period in the <xref:Silverback.Messaging.HealthChecks.ConsumersHealthCheck> to prevent false positives during a normal Kafka rebalance
* Add optional friendly name to the endpoints (see [IEndpointBuilder<TBuilder>.WithName](xref:Silverback.Messaging.Configuration.IEndpointBuilder`1#Silverback_Messaging_Configuration_IEndpointBuilder_1_WithName_System_String_) and [Endpoint.FriendlyName](xref:Silverback.Messaging.Endpoint#Silverback_Messaging_Endpoint_FriendlyName))
* Allow filtering the endpoints targeted by the <xref:Silverback.Messaging.HealthChecks.ConsumersHealthCheck> (see [AddConsumersCheck](xref:Microsoft.Extensions.DependencyInjection.HealthCheckBuilderExtensions#Microsoft_Extensions_DependencyInjection_HealthCheckBuilderExtensions_AddConsumersCheck_Microsoft_Extensions_DependencyInjection_IHealthChecksBuilder_Silverback_Messaging_Broker_ConsumerStatus_System_Nullable_System_TimeSpan__System_Func_Silverback_Messaging_IConsumerEndpoint_System_Boolean__System_String_System_Nullable_Microsoft_Extensions_Diagnostics_HealthChecks_HealthStatus__System_Collections_Generic_IEnumerable_System_String__))

## [3.1.1](https://github.com/BEagle1984/silverback/releases/tag/v3.1.1)

### Fixes

* Invoke broker callbacks during the application shutdown to allow custom code to be run when disconnecting

## [3.1.0](https://github.com/BEagle1984/silverback/releases/tag/v3.1.0)

### What's new

* Add new ways to configure headers and kafka key (see <xref:headers> and <xref:kafka-partitioning>)
* New callbacks for Kafka log events (see <xref:kafka-events>)
* Improve consumer status tracking introducing [ConsumerStatus.Ready](xref:Silverback.Messaging.Broker.ConsumerStatus)
    * Revert the Kafka consumer status from `Ready` to `Connected` whenever partitions are revoked or a poll timeout occurs
    * Adapt consumer health check to monitor the new status and report unhealthy if not `Ready` (see [Health Monitoring](xref:message-broker#health-monitoring))
* Try to automatically recover from Kafka maximum poll interval exceed errors
* Improve Kafka static partition assignment with resolver function and fetching the available partitions (see <xref:kafka-partitioning>)
* Upgrade to [Confluent.Kafka 1.7.0](https://github.com/confluentinc/confluent-kafka-dotnet/releases/tag/v1.7.0)
* Upgrade to [MQTTnet 3.0.15](https://github.com/chkr1011/MQTTnet/releases/tag/v3.0.15)

### Fixes

* Prevent possible race condition causing messages to be skipped when a `RetryPolicy` kicks in for messages from multiple Kafka partitions simultaneously
* Prevent `ObjectDisposedException` to be thrown when Kafka events (e.g. statistics) are fired during the application shutdown
* Prevent `ObjectDisposedException` to be thrown when `Consumer.Dispose` is called multiple times
* Properly clear the trace context (`Activity`) when reconnecting the consumer to prevent the newly started consume loop to be tracked under the current message traceId
* Fix wrong prefix in MQTT log event names 

## [3.0.1](https://github.com/BEagle1984/silverback/releases/tag/v3.0.1)

### Fixes

* Fix <xref:Silverback.Messaging.Outbound.TransactionalOutbox.Repositories.IOutboxWriter> lifecycle [[#128](https://github.com/BEagle1984/silverback/issues/128)]

## [3.0.0](https://github.com/BEagle1984/silverback/releases/tag/v3.0.0)

### What's new

* Add support for MQTT (see <xref:message-broker>, <xref:inbound>, <xref:outbound>, ...)
* Simplify configuration and reduce boilerplate (see <xref:subscribe> and <xref:message-broker>)
    * Simplify subscribers registration and get rid of the _ISubscriber_ interface (see <xref:subscribe>)
    * Scan subscribers automatically at startup to reduce cost of first message
    * Connect brokers and handle graceful shutdown automatically (see <xref:message-broker>)
    * Improve endpoints configuration API (see <xref:message-broker>)
    * Add [IServiceCollection.ConfigureSilverback](xref:Microsoft.Extensions.DependencyInjection.ServiceCollectionConfigureSilverbackExtensions) extension method to conveniently split the configuration code (see <xref:enabling-silverback>)
* Refactor Silverback.Integration to support streaming
    * Create <xref:Silverback.Messaging.Messages.IMessageStreamEnumerable`1> (see <xref:streaming>)
    * Improve chunking support in conjunction with streaming, requiring only one chunk at a time to be loaded into memory
    * Redesign sequences handling to support chunking, batch consuming and future sequences as well
* Improve Kafka partitions handling (see <xref:kafka-partitioning>)
    * Process partitions independently and concurrently
    * Add setting to produce to a specific partition
    * Add setting to manually assign the consumer partitions
* Add option to throw an exception if no subscriber is handling a message that was published to the internal bus or was consumed from a message broker (see `throwIfUnhandled` argument in the <xref:Silverback.Messaging.Publishing.IPublisher> methods and [ThrowIfUnhandled](xref:Silverback.Messaging.IConsumerEndpoint#Silverback_Messaging_IConsumerEndpoint_ThrowIfUnhandled) property in the <xref:Silverback.Messaging.IConsumerEndpoint>)
* Handle null messages as <xref:Silverback.Messaging.Messages.Tombstone>/<xref:Silverback.Messaging.Messages.Tombstone`1> (see <xref:tombstone>)
* Replace [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json) with [System.Text.Json](https://www.nuget.org/packages/System.Text.Json) to improve serialization and deserialization performance (the old serializers have been moved into the [Silverback.Integration.Newtonsoft](https://www.nuget.org/packages/Silverback.Integration.Newtonsoft) package, see <xref:serialization>)
* Improve outbound routing customization options with endpoint name resolvers (see <xref:outbound-routing>)
* Add non-blocking `Produce`/`ProduceAsync`/`RawProduce`/`RawProduceAsync` overloads to <xref:Silverback.Messaging.Broker.IProducer>, better suitable for higher throughput scenarios (see <xref:producer>)
* Refactor broker event handlers (see <xref:broker-callbacks>)
* Expose [IConsumer.StopAsync](xref:Silverback.Messaging.Broker.IConsumer#Silverback_Messaging_Broker_IConsumer_StopAsync) and [IConsumer.StartAsync](xref:Silverback.Messaging.Broker.IConsumer#Silverback_Messaging_Broker_IConsumer_StartAsync) methods to pause and resume consumers
* Add log levels configuration (see <xref:logging>)
* Improve (distributed) tracing (see <xref:logging>)
* Allow header names customization (see <xref:headers>)
* Add consumer status information and statistics (see <xref:message-broker#consumer-management-api>)
* Add basic consumer health check (see [Health Monitoring](xref:message-broker#health-monitoring))
* Allow broker behaviors to be registered as transient, meaning that an instance will be created per each producer or consumer (see <xref:broker-behaviors>)
* Improve code quality
    * Enhance CI pipeline to use Roslyn analyzers
    * Integrate [SonarCloud](https://sonarcloud.io/dashboard?id=silverback))
    * Improve integration tests
    * Increase automated tests coverage
    * Enable nullable reference types and adjust all API
    * Document the entire public API (see [API Documentation](~/api/Microsoft.Extensions.DependencyInjection.html))
* Released some utilities to help writing automated tests involving Silverback.Integration (see <xref:testing>)
* Upgrade to [Confluent.Kafka 1.6.2](https://github.com/confluentinc/confluent-kafka-dotnet/releases/tag/v1.6.2)
* Upgrade to [RabbitMQ.Client 6.2.1](https://github.com/rabbitmq/rabbitmq-dotnet-client/releases/tag/v6.2.1)

### Fixes

* Fix <xref:Silverback.Messaging.Outbound.TransactionalOutbox.OutboxWorker> not publishing custom headers [[#102](https://github.com/BEagle1984/silverback/issues/102)]

### Breaking Changes

* Refactored <xref:Silverback.Messaging.Publishing.IPublisher>
    * Removed the overloads to publish a batch of messages (see <xref:publish>)
    * Cannot subscribe to collection of messages anymore (see <xref:subscribe>), unless they are consumed from a message broker (see <xref:streaming>)
* The chunks belonging to the same message must be contiguous (interleaved messages are at the moment not supported anymore) and in the same partition in case of Kafka
* Removed _ISubscriber_ interface
* Removed _BusConfigurator_ (moved all the configuration into the <xref:Silverback.Messaging.Configuration.ISilverbackBuilder> extension methods)
    * Replaced _BusConfigurator.Connect_ with [ISilverbackBuilder.AddEndpointsConfigurator](xref:Microsoft.Extensions.DependencyInjection.SilverbackBuilderAddEndpointsConfiguratorExtensions) and [ISilverbackBuilder.AddEndpoints](xref:Microsoft.Extensions.DependencyInjection.SilverbackBuilderAddEndpointsExtensions) (or [ISilverbackBuilder.AddKafkaEndpoints](xref:Microsoft.Extensions.DependencyInjection.SilverbackBuilderAddKafkaEndpointsExtensions) etc.) to configure the endpoints, while the broker is connected automatically at startup (see <xref:message-broker>)
    * Replaced _BusConfigurator.Subscribe_ methods with [ISilverbackBuilder.AddDelegateSubscriber](xref:Microsoft.Extensions.DependencyInjection.SilverbackBuilderAddDelegateSubscriberExtensions) (see <xref:subscribe>)
    * Replaced _BusConfigurator.HandleMessagesOfType_ methods with [ISilverbackBuilder.HandleMessageOfType](xref:Silverback.Messaging.Configuration.SilverbackBuilderHandleMessageOfTypeExtensions) (see <xref:subscribe>)
    * _BusConfigurator.ScanSubscribers_ is not needed anymore since it gets called automatically at startup (from an [IHostedService](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.ihostedservice))
* Removed _IServiceCollection.Add*Subscriber_, _IServiceCollection.Add*Behavior_, _IServiceCollection.Add*BrokerBehavior_, _IServiceCollection.AddEndpointsConfigurator_, _IServiceCollection.Add*OutboundRouter_ extension methods, use the same methods on the <xref:Silverback.Messaging.Configuration.ISilverbackBuilder> (using [IServiceCollection.ConfigureSilverback](xref:Microsoft.Extensions.DependencyInjection.ServiceCollectionConfigureSilverbackExtensions) to get an instance if the <xref:Silverback.Messaging.Configuration.ISilverbackBuilder> if necessary, as shown in  <xref:enabling-silverback>)
* Removed _IBrokerOptionsBuilder.Add*BrokerBehavior_, _IBrokerOptionsBuilder.RegisterConfigurator_, _IBrokerOptionsBuilder.Add*OutboundRouter_ extension methods, use the same methods on the <xref:Silverback.Messaging.Configuration.ISilverbackBuilder> (using [IServiceCollection.ConfigureSilverback](xref:Microsoft.Extensions.DependencyInjection.ServiceCollectionConfigureSilverbackExtensions) to get an instance if the <xref:Silverback.Messaging.Configuration.ISilverbackBuilder> if necessary, as shown in  <xref:enabling-silverback>)
* Reorganized the `Silverback.Messaging.Configuration` namespace moving some broker specific types under `Silverback.Messaging.Configuration.Kafka`, `Silverback.Messaging.Configuration.Rabbit` or `Silverback.Messaging.Configuration.Mqtt`
* The visibility of some types has been changed to internal to favor a cleaner and clearer API where the public types are well documented and their backward compatibility is valued
* Removed _Silverback_ prefix from exceptions name
* Removed the _IRequest<TResponse>_ interface (it was implemented by both <xref:Silverback.Messaging.Messages.IQuery`1> and <xref:Silverback.Messaging.Messages.ICommand`1>)
* Changed _Impl_ methods suffix with _Core_, this affects some virtual members in the <xref:Silverback.Messaging.Broker.Broker`2> and other base classes
* _IConsumer.Received_ event replaced by a callback delegate
* _IBroker.GetConsumer_ and _IBrokerCollection.GetConsumer_ methods renamed to [IBroker.AddConsumer](xref:Silverback.Messaging.Broker.IBroker#Silverback_Messaging_Broker_IBroker_AddConsumer_Silverback_Messaging_IConsumerEndpoint_) and [IBrokerCollection.AddConsumer](xref:Silverback.Messaging.Broker.IBrokerCollection#Silverback_Messaging_Broker_IBrokerCollection_AddConsumer_Silverback_Messaging_IConsumerEndpoint_)
* _IQueueProducer_ and _IQueueConsumer_ renamed to <xref:Silverback.Messaging.Outbound.TransactionalOutbox.Repositories.IOutboxWriter> and <xref:Silverback.Messaging.Outbound.TransactionalOutbox.Repositories.IOutboxReader>
* The messages with a null body are by default mapped to a <xref:Silverback.Messaging.Messages.Tombstone>/<xref:Silverback.Messaging.Messages.Tombstone`1> (see <xref:tombstone>)
* Database:
    * Moved all entities (used with Entity Framework Core) to the `Silverback.Database.Model` namespace
    * Replaced _InboundMessage_ entity with <xref:Silverback.Database.Model.InboundLogEntry>
    * Replaced _OutboundMessage_ entity with <xref:Silverback.Database.Model.OutboxMessage>
    * Removed _TemporaryMessageChunk_
    * Modified schema of <xref:Silverback.Database.Model.StoredOffset> entity
* Moved and renamed some internally used types (e.g. _QueuedMessage_, _DbQueuedMessage_, ...)
* Complete redesign of the error policies
* Removed _IMessageIdProvider_ and all related logic: the `Id` or `MessageId` property will not be automatically initialized anymore and its value will not be used as identifier for the outbound message anymore (refer to the <xref:message-id> page for further details on how to set a custom message id, if needed)
* _WithConnectionTo<>_, _WithConnectionToKafka_, _WithConnectionToRabbitMQ_ and _WithInMemoryBroker_ have been removed, please use the new [WithConnectionToMessageBroker](xref:Microsoft.Extensions.DependencyInjection.SilverbackBuilderWithConnectionToExtensions) and [AddKafka](xref:Microsoft.Extensions.DependencyInjection.BrokerOptionsBuilderAddKafkaExtensions)/[AddRabbit](xref:Microsoft.Extensions.DependencyInjection.BrokerOptionsBuilderAddRabbitExtensions) methods (see <xref:message-broker>)
* Replaced the internal messages for the Kafka events such as partitions revoked/assigned, offset commit, error, log and statistics with event handler interfaces (see <xref:kafka-events>)
* Deprecated [Silverback.Integration.InMemory](https://www.nuget.org/packages/Silverback.Integration.InMemory), use [Silverback.Integration.Kafka.Testing](https://www.nuget.org/packages/Silverback.Integration.Kafka.Testing), [Silverback.Integration.RabbitMQ.Testing](https://www.nuget.org/packages/Silverback.Integration.RabbitMQ.Testing), etc. instead
* Renamed _PartitioningKeyMemberAttribute_ to <xref:Silverback.Messaging.Messages.KafkaKeyMemberAttribute>
* [Silverback.Integration.Configuration](https://www.nuget.org/packages/Silverback.Integration.Configuration) has been discontinued
* Renamed `Settings` property to `Options` in the default <xref:Silverback.Messaging.Serialization.JsonMessageSerializer> (since the switch to [System.Text.Json](https://www.nuget.org/packages/System.Text.Json))
* Removed `LogWithLevel` method from <xref:Silverback.Messaging.Inbound.ErrorHandling.SkipMessageErrorPolicy>, use the new `WithLogLevels` configuration instead
* Removed `Parallel` option from <xref:Silverback.Messaging.Subscribers.SubscribeAttribute>
* Renamed `Offset` to a more generic `BrokerMessageIdentifier` in the [Silverback.Integration](https://www.nuget.org/packages/Silverback.Integration) abstractions (including the envelopes)
* Some changes to the behaviors:
    * Renamed `Handle` to `HandleAsync` in the <xref:Silverback.Messaging.Publishing.IBehavior>, <xref:Silverback.Messaging.Broker.Behaviors.IProducerBehavior> and <xref:Silverback.Messaging.Broker.Behaviors.IConsumerBehavior>
    * Changed signature of the `HandleAsync` method (see <xref:behaviors> and <xref:broker-behaviors>)
    * Changed some sort indexes and introduced some new broker behaviors, you may need to adjust the sort index of your custom behaviors (see <xref:broker-behaviors> for the updated list of built-in behaviors)
* Replaced _IBroker.Connect_ and _IBroker.Disconnect_ with [IBroker.ConnectAsync](xref:Silverback.Messaging.Broker.IBroker#Silverback_Messaging_Broker_IBroker_ConnectAsync) and [IBroker.DisconnectAsync](xref:Silverback.Messaging.Broker.IBroker#Silverback_Messaging_Broker_IBroker_DisconnectAsync)
* Some major changes to batch consuming:
    * Removed all batch events (_BatchStartedEvent_, _BatchCompleteEvent_, _BatchProcessedEvent_, _BatchAbortedEvent_), refer to <xref:streaming> to learn how to leverage the new <xref:Silverback.Messaging.Messages.IMessageStreamEnumerable`1>
    * Setting the batch size to 1 doesn't disable batching anymore, set the `Batch` to `null` in the <xref:Silverback.Messaging.ConsumerEndpoint> to disable it
    * When batching is enabled the messages can be subscribed only via the <xref:Silverback.Messaging.Messages.IMessageStreamEnumerable`1> (see <xref:streaming>), the subscribers to the single messages will not be invoked
* <xref:Silverback.Messaging.Sequences.Chunking.ChunkSettings> moved from `Silverback.Messaging.LargeMessages` namespace to `Silverback.Messaging.Sequences.Chunking`
* Replaced _CoreEventIds_, _IntegrationEventIds_, _KafkaEventIds_ and _RabbitEventIds_ with <xref:Silverback.Diagnostics.CoreLogEvents>, <xref:Silverback.Diagnostics.IntegrationLogEvents>, <xref:Silverback.Diagnostics.KafkaLogEvents> and <xref:Silverback.Diagnostics.RabbitLogEvents> (see also <xref:logging>)
* Deprecated support for Entity Framework 2, only the version 3.0.1 of [Silverback.Core.EntityFrameworkCore](https://www.nuget.org/packages/Silverback.Core.EntityFrameworkCore) will work with Silverback 3.0.0
* Modified message encryption for chunked messages and it will not be compatible with previous versions of Silverback (affects chunking+encryption only)

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
* Add dynamic custom routing of outbound messages (see <xref:outbound-routing>)
* Improve support for message headers (see <xref:headers>)
* Add support for binary files (see <xref:binary-files>)
* Improve message identifier handling: the <xref:Silverback.Messaging.Messages.IIntegrationMessage> is not required to have an `Id` property anymore (the `x-message-id` header will still be generated and if the property exists will continue to be automatically initialized)
* `x-first-chunk-offset` header added by default (see <xref:headers>)
* Deserialize _KafkaStasticsEvent_ JSON and provided its content as an object (in addition to the raw JSON)
* Add support for [Apache Avro](https://avro.apache.org/) and schema registry (see <xref:serialization>)
* Upgrade to [Confluent.Kafka 1.4.2](https://github.com/confluentinc/confluent-kafka-dotnet/releases/tag/v1.4.2)
* Add consumer `PrefetchSize` and `PrefetchCount` settings to <xref:Silverback.Messaging.RabbitConsumerEndpoint>
* Add `AcknowledgeEach` to the <xref:Silverback.Messaging.RabbitConsumerEndpoint> to define the number of message processed before sending the acknowledgment to the server
* Upgrade to [RabbitMQ.Client 6.0.0](https://github.com/rabbitmq/rabbitmq-dotnet-client/releases/tag/v6.0.0)
* Improve message type resolution performance and reliability in <xref:Silverback.Messaging.Serialization.JsonMessageSerializer>
* Add `LogWithLevel` method to <xref:Silverback.Messaging.Inbound.ErrorHandling.SkipMessageErrorPolicy> to specify the desired level for the "Message skipped" log entry (the default is now increased to `Error`)

### Breaking Changes

These changes shouldn't affect you unless you built your own <xref:Silverback.Messaging.Broker.IBroker> implementation or are interacting at low-level with the <xref:Silverback.Messaging.Broker.IBroker> (this is why has been decided to still mark this as a minor release):

* The <xref:Silverback.Messaging.Broker.IBroker> interface and <xref:Silverback.Messaging.Broker.Broker`2> abstract base class have been modified to explicitly declare which endpoint type is being handled by the broker implementation
* The <xref:Silverback.Messaging.Serialization.IMessageSerializer> interfaces has been changed
* The <xref:Silverback.Messaging.Broker.Behaviors.IConsumerBehavior> and <xref:Silverback.Messaging.Broker.Behaviors.IProducerBehavior> interfaces have been changed
* Changed the parameters order in some less used overloads in the _IBrokerOptionBuilder_

### Announced Breaking Changes

These aren't real breaking changes but some methods have been marked as deprecated and will be removed in one of the next major releases:

* `WithConnectionTo<>`, `WithConnectionToKafka` and `WithConnectionToRabbitMQ` are deprecated (they will still be supported in this version), please use the new `WithConnectionToMessageBroker` and `AddKafka`/`AddRabbit` methods (see <xref:message-broker>)

## [2.0.0](https://github.com/BEagle1984/silverback/releases/tag/v2.0.0)

### What's new

* Create [Silverback.Integration.RabbitMQ](https://www.nuget.org/packages/Silverback.Integration.RabbitMQ) package to connect Silverback with RabbitMQ (see <xref:message-broker>)
* Enable subscription of messages with an empty body (you must subscribe to the <xref:Silverback.Messaging.Messages.IInboundEnvelope>) [[#61](https://github.com/BEagle1984/silverback/issues/61)]
* Add hook to manually set the Kafka partition start offset when a partition is assigned to the consumer (see <xref:kafka-events>) [[#57](https://github.com/BEagle1984/silverback/issues/57)]
* Support for multiple consumer groups running in the same process (see <xref:kafka-consumer-groups>) [[#59](https://github.com/BEagle1984/silverback/issues/59)]
* Publish _KafkaStatisticsEvent_ also from the <xref:Silverback.Messaging.Broker.KafkaProducer> (previously done in <xref:Silverback.Messaging.Broker.KafkaConsumer> only)
* Several reliability and performance related improvements

### Breaking Changes

* The <xref:Silverback.Messaging.Broker.IBroker>, <xref:Silverback.Messaging.Broker.IProducer> and <xref:Silverback.Messaging.Broker.IConsumer> interfaces have been slightly modified (it shouldn't affect you unless you built your own <xref:Silverback.Messaging.Broker.IBroker> implementation)
* Many interfaces (such as <xref:Silverback.Messaging.Publishing.IBehavior>) and delegates have been slightly modified to pass around an [IReadOnlyCollection<T>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlycollection-1) instead of an [IEnumerable<T>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1), to avoid the possible issues related to multiple enumeration of an [IEnumerable](https://docs.microsoft.com/en-us/dotnet/api/system.collections.ienumerable)
* The _IMessageKeyProvider_ interface has been renamed to _IMessageIdProvider_ to prevent to be mistaken with the Kafka Key or Rabbit's Routing Key
* _IInboundMessage_/_IOutboundMessage_ (plus all the related types) have been renamed to <xref:Silverback.Messaging.Messages.IInboundEnvelope>/<xref:Silverback.Messaging.Messages.IOutboundEnvelope> and the property containing the actual message has been renamed from `Content` to `Message`
* The `MustUnwrap` option has been removed from the inbound connector configuration (messages are unwrapped by default)

## [1.2.0](https://github.com/BEagle1984/silverback/releases/tag/v1.2.0)

### What's new

* Publish events to the internal bus as a consequence to the Kafka events such as partitions assigned or revoked (see <xref:kafka-events>) [[#34](https://github.com/BEagle1984/silverback/issues/34)]

## [1.1.0](https://github.com/BEagle1984/silverback/releases/tag/v1.1.0)

### What's new

* Add <xref:Silverback.Messaging.Configuration.IEndpointsConfigurator> interface to allow splitting the endpoints configuration across multiple types (see <xref:message-broker>)
* Add support for distributed tracing (based on [System.Diagnostics](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity?view=netcore-3.1))
* Add <xref:Silverback.Messaging.Broker.Behaviors.IProducerBehavior> and <xref:Silverback.Messaging.Broker.Behaviors.IConsumerBehavior> to create an extension point closer to the actual message broker logic (see <xref:broker-behaviors>)

### Breaking Changes

* Replaced _ISortedBehavior_ with a generic <xref:Silverback.ISorted> interface

## [1.0.5](https://github.com/BEagle1984/silverback/releases/tag/v1.0.5)

### What's new

* Upgrade to [Confluent.Kafka 1.3.0](https://github.com/confluentinc/confluent-kafka-dotnet/releases/tag/v1.3.0)

### Fixes

* Fix `OutboundQueueHealthCheck` [[#43](https://github.com/BEagle1984/silverback/issues/43)]
* Remove automatic disposal of the <xref:Silverback.Messaging.Broker.KafkaProducer> when a [KafkaException](https://docs.confluent.io/platform/current/clients/confluent-kafka-dotnet/api/Confluent.Kafka.KafkaException.html) is thrown (creating too many instances of the producer over a short time span could lead to too many active TCP connections)
* Fix the bug preventing a <xref:Silverback.Messaging.KafkaConsumerEndpoint> pointing to multiple topics to be successfully subscribed

## [1.0.4](https://github.com/BEagle1984/silverback/releases/tag/v1.0.4)

### Fixes

* Fix mortal loop issue: it is finally safe to consume and produce the same type of messages from within the same process (in a natural way, without any extra configuration)
    * Since version [1.0.0](#100) the messages routed to an endpoint aren't forwarded to any subscriber directly
    * Now the inbound connector has been fixed as well, preventing the inbound messages to be immediately routed once again to the outbound endpoint and eliminating all possible causes of mortal loops

## [1.0.3](https://github.com/BEagle1984/silverback/releases/tag/v1.0.3)

### What's new

* Deprecate _PartitioningKeyMemberAttribute_ in favor of <xref:Silverback.Messaging.Messages.KafkaKeyMemberAttribute>, since the message key isn't used just for partitioning (see <xref:kafka-partitioning>)

### Fixes

* Forward Kafka message key as-is (not hashed anymore) to avoid possible collisions and simplify debugging

## [1.0.2](https://github.com/BEagle1984/silverback/releases/tag/v1.0.2)

### Fixes

* Reintroduce `Add*Subscriber` and `Add*Behavior` as [IServiceCollection](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection) extension methods (for backward compatibility and greater flexibility) [[#41](https://github.com/BEagle1984/silverback/issues/41)]
* Add `WithInMemoryBroker` and `OverrideWithInMemoryBroker` extension methods (see <xref:testing>)

## [1.0.0](https://github.com/BEagle1984/silverback/releases/tag/v1.0.0)

### What's new

* Optimize message size (no wrappers anymore)
* Improve headers usage: identifiers, types, chunks information, etc. are now all sent in the headers
* Review severity of some log entries
* Improve and clean up internal implementation
* Improve exception handling (flattening of [AggregateException](https://docs.microsoft.com/en-us/dotnet/api/system.aggregateexception))
* Upgrade to [Confluent.Kafka 1.2.2](https://github.com/confluentinc/confluent-kafka-dotnet/releases/tag/v1.2.2)
* Add automatic recovers from fatal errors in <xref:Silverback.Messaging.Broker.KafkaConsumer> (can be disabled via Endpoint configuration)
* Support .Net Core 3.0 and Entity Framework Core 3.0
* Refactor packages (EF binding logic is now in a single package, versioned after the related EF version)
* Improve configuration API
* Improve and optimize performance (including [#37](https://github.com/BEagle1984/silverback/issues/37))
* Improve database locks mechanism (used also to run the _OutboundQueueWorker_)

### Fixes

* Fix issue requiring types not implementing <xref:Silverback.Messaging.Messages.IMessage> to be registered with `HandleMessagesOfType<T>` to consume them [[#33](https://github.com/BEagle1984/silverback/issues/33)]
* Mitigate issue causing the <xref:Silverback.Background.DistributedBackgroundService> to sometime fail to acquire the database lock [[#39](https://github.com/BEagle1984/silverback/issues/39)]
* Fix partition key value being lost when using the _DeferredOutboundConnector_
* Other small fixes to improve stability and reliability

### Breaking Changes

* By default the messages published via <xref:Silverback.Messaging.Publishing.IPublisher> that are routed to an outbound endpoint are not sent through to the internal bus and cannot therefore be subscribed locally, within the same process (see <xref:outbound>)
* Some changes in _IInboundMessage_ and _IOutboundMessage_ interfaces
* Changes to the schema of the outbox table (_Silverback.Messaging.Connectors.Model.OutboundMessage_)
* The configuration fluent API changed quite a bit, refer to the current documentation

> [!Important]
> `WithConnectionTo<KafkaBroker>` has to be replaced with `WithConnectionToKafka` in order for all features to work properly. When failing to do so no message key will be generated, causing the messages to land in a random partition and/or preventing to publish to a compacted topic. (see <xref:kafka-partitioning>)

* [Silverback.Integration.EntityFrameworkCore](https://www.nuget.org/packages/Silverback.Integration.EntityFrameworkCore) and [Silverback.EventSourcing.EntityFrameworkCore](https://www.nuget.org/packages/Silverback.EventSourcing.EntityFrameworkCore) have been deprecated ([Silverback.Core.EntityFrameworkCore](https://www.nuget.org/packages/Silverback.Core.EntityFrameworkCore) contains all the necessary logic to use EF as store)
* _KeyMemberAttribute_ has been renamed to _PartitioningKeyMemberAttribute_ (see <xref:message-broker>)

## [0.10.0](https://github.com/BEagle1984/silverback/releases/tag/v0.10.0)

### What's new

* Improve error handling: now all exceptions, including the ones thrown by the message serialzer can be handled through the error policies
* Improve logs: promoted some important logs to Information level, writing all processing errors as (at least) Warning and improved logged information quality (logged attributes)
* Add ability to modify messages and headers when moving them via <xref:Silverback.Messaging.Inbound.ErrorHandling.MoveMessageErrorPolicy>
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

* Add support for message headers
* Simplify message subscription even further: now all public methods of the types implementing the marker interface _ISubscriber_ are automatically subscribed by default without having to annotate them with the <xref:Silverback.Messaging.Subscribers.SubscribeAttribute> (this behavior is customizable)
* Upgrade to [Confluent.Kafka 1.0.0-RC1](https://github.com/confluentinc/confluent-kafka-dotnet/releases/tag/v1.0-RC1)

## 0.3.x - 0.5.x

Some releases where done adding quite a few features.

### What's new

* Add Silverback.Integration.Configuration package to load the inbound/outbound configuration from the app.settings json
* Add batch processing
* Add parallel subscribers
* Add delegate subscription as an alternative to <xref:Silverback.Messaging.Subscribers.SubscribeAttribute> based subscription
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
