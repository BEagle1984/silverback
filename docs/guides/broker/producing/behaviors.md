---
uid: producer-behaviors
---

# Producer Behaviors Pipeline

Each message produced by Silverback is processed by a pipeline of behaviors enabling different features and customizations. The `SortIndex` property of each behavior determines the order in which they are executed and each behavior passes the message along the pipeline to the next behavior.

## Default Behaviors

The following table lists the default behaviors that are executed when producing a message.

Name | Index | Description
:-- |------:| :--
<xref:Silverback.Messaging.Diagnostics.ActivityProducerBehavior> |   100 | Starts an [Activity](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity) and adds the tracing information to the message headers.
<xref:Silverback.Messaging.Headers.HeadersWriterProducerBehavior> |   200 | Maps the properties decorated with the <xref:Silverback.Messaging.Messages.HeaderAttribute> to the message headers.
<xref:Silverback.Messaging.Producing.Enrichers.MessageEnricherProducerBehavior> |   250 | Invokes all the <xref:Silverback.Messaging.Producing.Enrichers.IOutboundMessageEnricher> configured for to the endpoint.
<xref:Silverback.Messaging.Producing.KafkaMessageKeyInitializerProducerBehavior> |   300 | Sets the Kafka key with the value from the properties decorated with the `KafkaKeyMemberAttribute` and ensures that a random key is generated when chunking is enabled and no key is set.
<xref:Silverback.Messaging.Producing.Filter.FilterProducerBehavior> |   400 | Applies the configured filters to the message being produced.
<xref:Silverback.Messaging.BinaryMessages.BinaryMessageHandlerProducerBehavior> |   500 | Switches to the <xref:Silverback.Messaging.BinaryMessages.BinaryMessageSerializer> if the message being produced implements the <xref:Silverback.Messaging.Messages.IBinaryMessage> interface.
<xref:Silverback.Messaging.Validation.ValidatorProducerBehavior> | 550 | Validates the message being produced.
<xref:Silverback.Messaging.Serialization.SerializerProducerBehavior> | 600 | Serializes the message being produced using the configured <xref:Silverback.Messaging.Serialization.IMessageSerializer>.
<xref:Silverback.Messaging.Encryption.EncryptorProducerBehavior> |   700 | Encrypts the message when encryption is enabled.
<xref:Silverback.Messaging.Sequences.SequencerProducerBehavior> | 800 | Uses the available implementations of <xref:Silverback.Messaging.Sequences.ISequenceWriter> (e.g. <xref:Silverback.Messaging.Sequences.Chunking.ChunkSequenceWriter>) to set the proper headers and split the published message or messages set to create the sequences.
<xref:Silverback.Messaging.Headers.CustomHeadersMapperProducerBehavior> |  1000 | Applies the custom header name mappings.

## Custom Behaviors

It is possible to add custom behaviors to the pipeline to implement cross-cutting concerns, and add new features to Silverback.

A behavior is a class implementing the `IProducerBehavior` interface. The `HandleAsync` method is called for each message being produced, allowing the behavior to modify the message or perform additional operations.

The following basic example demonstrates how to set a custom message header on each outbound message.

```csharp
public class CustomHeadersProducerBehavior : IProducerBehavior
{
    public int SortIndex => 1000;

    public async ValueTask HandleAsync(
        ProducerPipelineContext context,
        ProducerBehaviorHandler next,
        CancellationToken cancellationToken)
    {
        context.Envelope.Headers.Add("generated-by", "silverback");

        await next(context);
    }
}
```

The custom behavior must be registered at startup using the `AddSingletonBrokerBehavior`, or `AddTransientBrokerBehavior` method. The transient behavior will be resolved once per producer, while the singleton behavior will be shared.

```csharp
services
    .AddSilverback()
    ...
    .AddSingletonBrokerBehavior<CustomHeadersProducerBehavior>();
```

Additional services can be injected into the behavior constructor, or resolved via the `ServiceProvider` from the <xref:Silverback.Messaging.Producing.ProducerPipelineContext> if a scoped instance is required.

## Additional Resources

* [API Reference](xref:Silverback)
* <xref:consumer-behaviors> guide
