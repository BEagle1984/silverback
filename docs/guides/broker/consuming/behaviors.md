---
uid: consumer-behaviors
---

# Consumer Behaviors Pipeline

Each message consumed by Silverback is processed by a pipeline of behaviors that enable features and cross-cutting concerns. The `SortIndex` property of each behavior determines the order in which they are executed and each behavior must call the next behavior in the pipeline to continue processing.

## Default Behaviors

The following table lists the default behaviors that are executed when consuming a message.

| Name                                                                 | Index | Description                                                                                                                                                       |
| :------------------------------------------------------------------- | ----: | :---------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| <xref:Silverback.Messaging.Diagnostics.ActivityConsumerBehavior>    |   100 | Starts an [Activity](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity) for the incoming message and extracts tracing information from headers when available. |
| <xref:Silverback.Messaging.Diagnostics.FatalExceptionLoggerConsumerBehavior> |   200 | Logs fatal exceptions thrown while processing the message (used to flag unrecoverable errors and improve observability).                                        |
| <xref:Silverback.Messaging.Headers.CustomHeadersMapperConsumerBehavior> |   300 | Maps message headers to the destination names configured with the custom header mappings.                                                                        |
| <xref:Silverback.Messaging.Consuming.Transaction.TransactionHandlerConsumerBehavior> |   400 | Manages the transaction scope for the message processing, using the configured <xref:Silverback.Messaging.Consuming.Transaction.IConsumerTransactionManager> implementations. |
| <xref:Silverback.Messaging.Sequences.RawSequencerConsumerBehavior> |   500 | Handles raw sequence-related headers (for messages published as sequences) and prepares the message for sequencing logic.                                        |
| <xref:Silverback.Messaging.Encryption.DecryptorConsumerBehavior>   |   700 | Decrypts the message payload when encryption is enabled on the endpoint.                                                                                       |
| <xref:Silverback.Messaging.BinaryMessages.BinaryMessageHandlerConsumerBehavior> |   800 | Switches to the <xref:Silverback.Messaging.BinaryMessages.BinaryMessageSerializer> when the consumed message implements <xref:Silverback.Messaging.Messages.IBinaryMessage> and handles binary payloads. |
| <xref:Silverback.Messaging.Serialization.DeserializerConsumerBehavior> |   900 | Deserializes the message payload into the target CLR type using the configured <xref:Silverback.Messaging.Serialization.IMessageSerializer>.                     |
| <xref:Silverback.Messaging.Validation.ValidatorConsumerBehavior>    |   950 | Validates the deserialized message using the configured validation rules and validators.                                                                          |
| <xref:Silverback.Messaging.Headers.HeadersReaderConsumerBehavior>   |  1000 | Reads headers and maps them to message properties decorated with the <xref:Silverback.Messaging.Messages.HeaderAttribute>.                                      |
| <xref:Silverback.Messaging.Sequences.SequencerConsumerBehavior>    | 1100 | Uses the available implementations of <xref:Silverback.Messaging.Sequences.ISequenceReader> to reassemble sequences (e.g., chunked messages) and pass the resulting message(s) down the pipeline. |
| <xref:Silverback.Messaging.Consuming.PublisherConsumerBehavior>    | 2000 | Publishes the message to the configured message handlers, in-memory subscribers or any other final destinations.                                                  |

## Custom Behaviors

It is possible to add custom behaviors to the consumer pipeline to implement cross-cutting concerns or extend Silverback's processing. A consumer behavior is a class implementing the `IConsumerBehavior` interface. The `HandleAsync` method is invoked for each consumed message, allowing the behavior to modify the envelope, perform side-effects, or short-circuit the pipeline.

```csharp
public class CustomConsumerBehavior : IConsumerBehavior
{
    public int SortIndex => 300;

    public async ValueTask HandleAsync(
        ConsumerPipelineContext context,
        ConsumerBehaviorHandler next,
        CancellationToken cancellationToken)
    {
        // ...

        await next(context);
    }
}
```

The custom behavior must be registered at startup using the `AddSingletonBrokerBehavior`, or `AddTransientBrokerBehavior` method. The transient behavior will be resolved once per producer, while the singleton behavior will be shared.

```csharp
services
    .AddSilverback()
    ...
    .AddSingletonBrokerBehavior<CustomHeadersConsumerBehavior>();
```

Additional services can be injected into the behavior constructor or resolved from the `ServiceProvider` available on the <xref:Silverback.Messaging.Broker.Behaviors.ConsumerPipelineContext> when a scoped instance is required.

## Additional Resources

- [API Reference](xref:Silverback)
- <xref:producer-behaviors> guide
