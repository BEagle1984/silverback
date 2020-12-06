---
uid: broker-behaviors
---

# Broker behaviors pipeline

Silverback is built to be modular and most of its feature are plugged into the consumers and producers via some so-called behaviors. The inbound and outbound messages flow through this pipeline and each behavior take care of a specific task such as serialization, encryption, chunking, logging, etc. 

The <xref:Silverback.Messaging.Broker.Behaviors.IProducerBehavior> and <xref:Silverback.Messaging.Broker.Behaviors.IConsumerBehavior> are the interfaces used to build such behaviors.

> [!Note]
> <xref:Silverback.Messaging.Broker.Behaviors.IProducerBehavior> and <xref:Silverback.Messaging.Broker.Behaviors.IConsumerBehavior> inherit the <xref:Silverback.ISorted> interface. It is therefore mandatory to specify the exact sort index of each behavior.

## Built-in producer behaviors

This behaviors build the producer pipeline and contain the actual logic to properly serialize the messages according to the applied configuration.

Name | Index | Description
:-- | --: | :--
<xref:Silverback.Messaging.Diagnostics.ActivityProducerBehavior> | 100 | Starts an [Activity](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity) and adds the tracing information to the message headers.
<xref:Silverback.Messaging.Headers.HeadersWriterProducerBehavior> | 200 | Maps the properties decorated with the <xref:Silverback.Messaging.Messages.HeaderAttribute> to the message headers.
<xref:Silverback.Messaging.Broker.Behaviors.MessageIdInitializerProducerBehavior> | 300 | It ensures that an `x-message-id` header is always produced.
BrokerKeyHeaderInitializer | 400 | Provided by the message broker implementation (e.g. <xref:Silverback.Messaging.Behaviors.KafkaMessageKeyInitializerProducerBehavior> or <Silverback.Messaging.Behaviors.RabbitRoutingKeyInitializerProducerBehavior>), sets the message key header that will be used by the <xref:Silverback.Messaging.Broker.IProducer> implementation to set the actual message key.
<xref:Silverback.Messaging.BinaryFiles.BinaryFileHandlerProducerBehavior> | 500 | Switches to the <xref:Silverback.Messaging.BinaryFiles.BinaryFileMessageSerializer> if the message being produced implements the <xref:Silverback.Messaging.Messages.IBinaryFileMessage> interface.
<xref:Silverback.Messaging.Serialization.SerializerProducerBehavior> | 600 | Serializes the message being produced using the configured <xref:Silverback.Messaging.Serialization.IMessageSerializer>.
<xref:Silverback.Messaging.Encryption.EncryptorProducerBehavior> | 700 | Encrypts the message according to the <xref:Silverback.Messaging.Encryption.EncryptionSettings>.
<xref:Silverback.Messaging.Sequences.SequencerProducerBehavior> | 800 | Uses the available implementations of <xref:Silverback.Messaging.Sequences.ISequenceWriter> (e.g. <xref:Silverback.Messaging.Sequences.Chunking.ChunkSequenceWriter>) to set the proper headers and split the published message or messages set to create the sequences.
<xref:Silverback.Messaging.Headers.CustomHeadersMapperProducerBehavior> | 1000 | Applies the custom header name mappings.

## Built-in consumer behaviors

This behaviors are the foundation of the consumer pipeline and contain the actual logic to deserialize the incoming messages.

Name | Index | Description
:-- | --: | :--
<xref:Silverback.Messaging.Diagnostics.ActivityConsumerBehavior> | 100 | Starts an [Activity](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity) with the tracing information from the message headers.
<xref:Silverback.Messaging.Diagnostics.FatalExceptionLoggerConsumerBehavior> | 200 | Logs the unhandled exceptions thrown while processing the message. These exceptions are fatal since they will usually cause the consumer to stop.
<xref:Silverback.Messaging.Headers.CustomHeadersMapperConsumerBehavior> | 300 | Applies the custom header name mappings.
<xref:Silverback.Messaging.Inbound.Transaction.TransactionHandlerConsumerBehavior> | 400 | Handles the consumer transaction and applies the error policies.
<xref:Silverback.Messaging.Sequences.RawSequencerConsumerBehavior> | 500 | Uses the available implementations of <xref:Silverback.Messaging.Sequences.ISequenceReader> (e.g. <xref:Silverback.Messaging.Sequences.Chunking.ChunkSequenceReader>) to assign the incoming message to the right sequence.
<xref:Silverback.Messaging.Inbound.ExactlyOnce.ExactlyOnceGuardConsumerBehavior> | 600 | Uses the configured implementation of <xref:Silverback.Messaging.Inbound.ExactlyOnce.IExactlyOnceStrategy> to ensure that the message is processed only once.
<xref:Silverback.Messaging.Encryption.DecryptorConsumerBehavior> | 700 | Decrypts the message according to the <xref:Silverback.Messaging.Encryption.EncryptionSettings>.
<xref:Silverback.Messaging.BinaryFiles.BinaryFileHandlerProducerBehavior> | 800 | Switches to the <xref:Silverback.Messaging.BinaryFiles.BinaryFileMessageSerializer> if the message being consumed is a binary message (according to the `x-message-type` header.
<xref:Silverback.Messaging.Serialization.DeserializerConsumerBehavior> | 900 | Deserializes the messages being consumed using the configured <xref:Silverback.Messaging.Serialization.IMessageSerializer>.
<xref:Silverback.Messaging.Headers.HeadersReaderConsumerBehavior> | 1000 | Maps the headers with the properties decorated with the <xref:Silverback.Messaging.Messages.HeaderAttribute>.
<xref:Silverback.Messaging.Sequences.SequencerConsumerBehavior> | 1100 | Uses the available implementations of <xref:Silverback.Messaging.Sequences.ISequenceReader> (e.g. <xref:Silverback.Messaging.Sequences.Batch.BatchSequenceReader>) to assign the incoming message to the right sequence.
<xref:Silverback.Messaging.Inbound.PublisherConsumerBehavior> | 2000 | Publishes the consumed messages to the internal bus.

## Custom behaviors

The behaviors can be used to implement cross-cutting concerns or add new features to Silverback.

### Custom IProducerBehavior example

The following example demonstrate how to set a custom message header on each outbound message.

> [!Note]
> The <xref:Silverback.Messaging.Broker.Behaviors.ProducerPipelineContext> and <xref:Silverback.Messaging.Broker.Behaviors.ConsumerPipelineContext> hold a reference to the [IServiceProvider](https://docs.microsoft.com/en-us/dotnet/api/system.iserviceprovider) and can be used to resolve the needed services.
> The [IServiceProvider](https://docs.microsoft.com/en-us/dotnet/api/system.iserviceprovider) in the <xref:Silverback.Messaging.Broker.Behaviors.ConsumerPipelineContext> can be either the root service provider or the scoped service provider for the processing of the consumed message (depending on the position of the behavior in the pipeline).

> [!Note]
> The broker behaviors can be registered either as singleton or transient services. When registered as transient a new instance will be created per each producer or consumer.

# [ProducerBehavior](#tab/producerbehavior)
```csharp
public class CustomHeadersProducerBehavior : IProducerBehavior
{
    public int SortIndex => 1000;

    public async Task HandleAsync(
        ProducerPipelineContext context, 
        ProducerBehaviorHandler next)
    {
        context.Envelope.Message.Headers.Add("generated-by", "silverback");

        await next(context);
    }
}
```
# [Startup](#tab/producerbehavior-startup)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka())
            .AddSingletonBrokerBehavior<CustomHeadersBehavior>();
    }
}
```
***

### Custom IConsumerBehavior example

The following example demonstrate how to log the headers received with each inbound message.

# [ConsumerBehavior](#tab/consumerbehavior)
```csharp
public class LogHeadersConsumerBehavior : IConsumerBehavior
{
    private readonly ILogger<LogHeadersBehavior> _logger;

    public LogHeadersBehavior(ILogger<LogHeadersBehavior> logger)
    {
        _logger = logger;
    }

    public int SortIndex => 1000;

    public async Task HandleAsync(
        ConsumerPipelineContext context, 
        ConsumerBehaviorHandler next)
    {
        foreach (var envelope in context.Envelopes)
        {
            foreach (var header in envelope.Headers)
            {
                _logger.LogTrace(
                    "{key}={value}",
                    header.Key,
                    header.Value);
            }
        }

        await next(context);
    }
}
```
# [Startup](#tab/consumerbehavior-startup)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka())
            .AddSingletonBrokerBehavior<LogHeadersBehavior>();
    }
}
```
***

## See also

<xref:behaviors>
