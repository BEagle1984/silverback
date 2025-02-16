---
uid: binary-files
---

# Binary Files

Serializing a binary file (a stream or a byte array) using the regular <xref:Silverback.Messaging.Serialization.JsonMessageSerializer> would mean to encode it in base64 and convert it to a UTF-8 encoded byte array. Beside not being very elegant this approach may cause you some trouble when integrating with other systems expecting the raw file content. This procedure would also result in the transferred byte array to be approximately a 30% bigger than the file itself.

In this page it's shown how to use an <xref:Silverback.Messaging.Messages.IBinaryFileMessage> to more efficiently transfer raw binary files.

## Producer configuration

The <xref:Silverback.Messaging.Messages.IBinaryFileMessage> interface is meant to transfer files over the message broker and is natively supported by Silverback. This means that the raw file content will be transferred in its original form.

For convenience the <xref:Silverback.Messaging.Messages.BinaryFileMessage> class already implements the <xref:Silverback.Messaging.Messages.IBinaryFileMessage> interface. This class exposes a `ContentType` property as well, resulting in the `content-type` [header](xref:headers) to be produced.

# [EndpointsConfigurator (fluent)](#tab/producer-fluent)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                    {
                        config.BootstrapServers = "PLAINTEXT://kafka:9092"; 
                    })
                .AddOutbound<IBinaryFileMessage>(endpoint => endpoint
                    .ProduceTo("raw-files")));
}
```
# [EndpointsConfigurator (legacy)](#tab/producer-legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddOutbound<IBinaryFileMessage>(
                new KafkaProducerEndpoint("inventory-events")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092"
                    } 
                });
}
```
# [Publisher](#tab/producer-service)
```csharp
public class FileTransferService
{
    private readonly IPublisher _publisher;

    public FileTransferService(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task TransferFile(byte[] content, string contentType)
    {
        await _publihser.PublishAsync(
            new BinaryFileMessage(content, contentType));
    }
}
```
***

Otherwise you can implement the interface yourself or extend the <xref:Silverback.Messaging.Messages.BinaryFileMessage> (e.g. to add some additional headers, as explained in the <xref:headers> section).

# [EndpointsConfigurator (fluent)](#tab/producer-custom-fluent)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                    {
                        config.BootstrapServers = "PLAINTEXT://kafka:9092"; 
                    })
                .AddOutbound<IBinaryFileMessage>(endpoint => endpoint
                    .ProduceTo("raw-files")));
}
```
# [EndpointsConfigurator (legacy)](#tab/producer-custom-legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddOutbound<IBinaryFileMessage>(
                new KafkaProducerEndpoint("raw-files")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092"
                    } 
                });
}
```
# [Message](#tab/producer-custom-model)
```csharp
public class MyBinaryFileMessage : BinaryFileMessage
{
    [Header("x-user-id")]
    public Guid UserId { get; set; }
}
```
# [Publisher](#tab/producer-custom-service)
```csharp
public class FileTransferService
{
    private readonly IPublisher _publisher;

    public FileTransferService(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task TransferFile(
        byte[] content, 
        string contentType,
        Guid userId)
    {
        await _publihser.PublishAsync(
            new MyBinaryFileMessage
            {
                Content = content,
                ContentType = contentType,
                UserId = userId
            });
    }
}
```
***

## Consumer configuration

You don't need to do anything special to consume a binary file, if all necessary headers are in place (ensured by Silverback, if it was used to produce the message). The message will be wrapped again in a <xref:Silverback.Messaging.Messages.BinaryFileMessage> that can be subscribed like any other message.

# [EndpointsConfigurator (fluent)](#tab/consumer-fluent)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                    {
                        config.BootstrapServers = "PLAINTEXT://kafka:9092"; 
                    })
                .AddInbound(endpoint => endpoint
                    .ConsumeFrom("raw-files")
                    .Configure(config =>
                        {
                            config.GroupId = "my-consumer"
                        }));
}
```
# [EndpointsConfigurator (legacy)](#tab/consumer-legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddInbound(
                new KafkaConsumerEndpoint("raw-files")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092"
                    } 
                });
}
```
# [Subscriber](#tab/consumer-subscriber)
```csharp
public class FileSubscriberService
{
    public async Task OnFileReceived(IBinaryFileMessage message)
    {
        // ...your file handling logic...
    }
}
```
***

If the message wasn't produced by Silverback chances are that the message type header is not there. In that case you need to explicitly configure the <xref:Silverback.Messaging.BinaryFiles.BinaryFileMessageSerializer> in the inbound endpoint.

# [EndpointsConfigurator (fluent)](#tab/consumer-explicit-fluent)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                    {
                        config.BootstrapServers = "PLAINTEXT://kafka:9092"; 
                    })
                .AddInbound(endpoint => endpoint
                    .ConsumeFrom("raw-files")
                    .ConsumeBinaryFiles()
                    .Configure(config =>
                        {
                            config.GroupId = "my-consumer"
                        }));
}
```
# [EndpointsConfigurator (legacy)](#tab/consumer-explicit-legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddInbound(
                new KafkaConsumerEndpoint("raw-files")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092"
                    } ,
                    Serializer = BinaryFileMessageSerializer.Default
                });
}
```
# [Subscriber](#tab/consumer-explicit-subscriber)
```csharp
public class FileSubscriberService
{
    public async Task OnFileReceived(IBinaryFileMessage message)
    {
        // ...your file handling logic...
    }
}
```
***

If you need to read additional headers you can either extend the <xref:Silverback.Messaging.Messages.BinaryFileMessage> (suggested approach) or subscribe to an <xref:Silverback.Messaging.Messages.IInboundEnvelope`1> .

The following snippet assumes that the files aren't being streamed by a Silverback producer, otherwise it wouldn't be necessary to explicitly set the serializer and the type would be inferred from the `x-message-type` header.

# [EndpointsConfigurator (fluent)](#tab/consumer-custom-fluent)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                    {
                        config.BootstrapServers = "PLAINTEXT://kafka:9092"; 
                    })
                .AddInbound(endpoint => endpoint
                    .ConsumeFrom("raw-files")
                    .ConsumeBinaryFiles(serializer => serializer.UseModel<MyBinaryFileMessage>()));
}
```
# [EndpointsConfigurator (legacy)](#tab/consumer-custom-legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddOutbound<IBinaryFileMessage>(
                new KafkaProducerEndpoint("raw-files")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092"
                    },
                    Serializer = new BinaryFileMessageSerializer<MyBinaryFileMessage>()
                });
}
```
# [Message](#tab/consumer-custom-model)
```csharp
public class MyBinaryFileMessage : BinaryFileMessage
{
    [Header("x-user-id")]
    public Guid UserId { get; set; }
}
```
# [Subscriber](#tab/consumer-custom-subscriber)
```csharp
public class FileSubscriberService
{
    public async Task OnFileReceived(MyBinaryFileMessage message)
    {
        // ...your file handling logic...
    }
}
```
***

## Samples

* <xref:example-kafka-binaryfile>
