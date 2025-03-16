---
uid: default-headers
---

# Default Headers

Silverback will add some headers to the produced messages. They may vary depending on the scenario.
Here is the list of the default headers that may be sent.

The static classes <xref:Silverback.Messaging.Messages.DefaultMessageHeaders> and <xref:Silverback.Messaging.Messages.KafkaMessageHeaders> contain all default header names constants.

Header | Name
:-- | :--
[MessageId](xref:Silverback.Messaging.Messages.DefaultMessageHeaders#Silverback_Messaging_Messages_DefaultMessageHeaders_MessageId) | `x-message-id`
[MessageType](xref:Silverback.Messaging.Messages.DefaultMessageHeaders#Silverback_Messaging_Messages_DefaultMessageHeaders_MessageType) | `x-message-type`
[FailedAttempts](xref:Silverback.Messaging.Messages.DefaultMessageHeaders#Silverback_Messaging_Messages_DefaultMessageHeaders_FailedAttempts) | `x-failed-attempts`
[ChunkIndex](xref:Silverback.Messaging.Messages.DefaultMessageHeaders#Silverback_Messaging_Messages_DefaultMessageHeaders_ChunkIndex) | `x-chunk-index`
[ChunksCount](xref:Silverback.Messaging.Messages.DefaultMessageHeaders#Silverback_Messaging_Messages_DefaultMessageHeaders_ChunksCount) | `x-chunk-count`
[IsLastChunk](xref:Silverback.Messaging.Messages.DefaultMessageHeaders#Silverback_Messaging_Messages_DefaultMessageHeaders_IsLastChunk) | `x-chunk-last`
[TraceId](xref:Silverback.Messaging.Messages.DefaultMessageHeaders#Silverback_Messaging_Messages_DefaultMessageHeaders_TraceId) | `traceparent`
[TraceState](xref:Silverback.Messaging.Messages.DefaultMessageHeaders#Silverback_Messaging_Messages_DefaultMessageHeaders_TraceState) | `tracestate`
[TraceBaggage](xref:Silverback.Messaging.Messages.DefaultMessageHeaders#Silverback_Messaging_Messages_DefaultMessageHeaders_TraceBaggage) | `tracebaggage`
[ContentType](xref:Silverback.Messaging.Messages.DefaultMessageHeaders#Silverback_Messaging_Messages_DefaultMessageHeaders_ContentType) | `content-type`
[EncryptionKeyId](xref:Silverback.Messaging.Messages.DefaultMessageHeaders#Silverback_Messaging_Messages_DefaultMessageHeaders_EncryptionKeyId) | `x-encryption-key-id`
[FailureReason](xref:Silverback.Messaging.Messages.DefaultMessageHeaders#Silverback_Messaging_Messages_DefaultMessageHeaders_FailureReason) | `x-failure-reason`

## Kafka Specific Headers

Header | Name
:-- | :--
[Timestamp](xref:Silverback.Messaging.Messages.KafkaMessageHeaders#Silverback_Messaging_Messages_KafkaMessageHeaders_Timestamp) | `x-kafka-message-timestamp`
[SourceConsumerGroupId](xref:Silverback.Messaging.Messages.KafkaMessageHeaders#Silverback_Messaging_Messages_KafkaMessageHeaders_SourceConsumerGroupId) | `x-source-consumer-group-id`
[SourceTopic](xref:Silverback.Messaging.Messages.KafkaMessageHeaders#Silverback_Messaging_Messages_KafkaMessageHeaders_SourceTopic) | `x-source-topic`
[SourcePartition](xref:Silverback.Messaging.Messages.KafkaMessageHeaders#Silverback_Messaging_Messages_KafkaMessageHeaders_SourcePartition) | `x-source-partition`
[SourceOffset](xref:Silverback.Messaging.Messages.KafkaMessageHeaders#Silverback_Messaging_Messages_KafkaMessageHeaders_SourceOffset) | `x-source-offset`
[SourceTimestamp](xref:Silverback.Messaging.Messages.KafkaMessageHeaders#Silverback_Messaging_Messages_KafkaMessageHeaders_SourceTimestamp) | `x-source-timestamp`
[FirstChunkOffset](xref:Silverback.Messaging.Messages.KafkaMessageHeaders#Silverback_Messaging_Messages_KafkaMessageHeaders_FirstChunkOffset) | `x-chunk-first-offset`

## Customizing Default Header Names

The default header names can be overridden using the `WithCustomHeaderName` configuration method.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .WithCustomHeaderName(DefaultMessageHeaders.ChunkId, "x-ch-id")
    .WithCustomHeaderName(DefaultMessageHeaders.ChunksCount, "x-ch-cnt")
    .AddBrokerClientsConfigurator<MyClientsConfigurator>();
```
