---
uid: binary-files
---

# Binary Files

Serializing a binary file (a byte array) using the regular `JsonMessageSerializer` would mean to encode it in base64 and convert it to a UTF-8 encoded byte array. Beside not being very elegant this approach may cause you some trouble when integrating with other systems expecting the raw file content. This procedure would also result in the transferred byte array to be approximately a 30% bigger than the file itself.

In this page it's shown how to use an `IBinaryFileMessage` to more efficiently transfer binary files.

## Producing using IBinaryFileMessage

The `IBinaryFileMessage` interface is meant to transfer files over the message broker and is natively supported by Silverback. This means that the raw file content will be transferred in its original form.

For convenience the `BinaryFileMessage` class already implements the `IBinaryFileMessage` interface. This class exposes a `ContentType` property as well, resulting in the `content-type` [header](xref:headers) to be produced.

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

Otherwise you can implement the interface yourself or extend the `BinaryFileMessage` (e.g. to add some additional headers, as explained in the <xref:headers> section of the quickstart).

```csharp
public class MyBinaryFileMessage : BinaryFileMessage
{
    [Header("x-user-id")]
    public Guid UserId { get; set; }
}
```

## Consuming an IBinaryFileMessage

You don't need to do anything special to consume a binary file, if all necessary headers are in place (ensured by Silverback, if it was used to produce the message). The message will be wrapped again in a `BinaryFileMessage` that can be subscribed like any other message.

```csharp
public class FileConsumerService : ISubscriber
{
    public async Task OnFileReceived(IBinaryFileMessage message)
    {
        // ...your file handling logic...
    }
}
```

If the message wasn't produced by Silverback chances are that the message type header is not there. In that case you need to explicitly configure the `BinaryFileMessageSerializer` in the inbound endpoint.

```csharp
public class Startup
{
    public void Configure(IBusConfigurator busConfigurator)
    {
        busConfigurator
            .Connect(endpoints => endpoints
                .AddInbound(
                    new KafkaConsumerEndpoint("basket-events")
                    {
                        Serializer = BinaryFileMessageSerializer.Default
                    }));
    }
}
```

If you need to read additional headers you can either subscribe to an `IInboundEnvelope<BinaryFileMessage>` or extend the `BinaryFileMessage` (or implement the `IBinaryFileMessage` interface from scratch). In this case you need to adapt the serializer configuration as well.

# [Message Model](#tab/custommodel)
```csharp
public class MyBinaryFileMessage : BinaryFileMessage
{
    [Header("x-user-id")]
    public Guid UserId { get; set; }
}
```
# [Startup](#tab/custommodel-startup)
```csharp
public class Startup
{
    public void Configure(IBusConfigurator busConfigurator)
    {
        busConfigurator
            .Connect(endpoints => endpoints
                .AddInbound(
                    new KafkaConsumerEndpoint("basket-events")
                    {
                        Serializer = new BinaryFileMessageSerializer<MyBinaryFileMessage>()
                    }));
    }
}
```
