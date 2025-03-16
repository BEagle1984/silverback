---
uid: encryption
---

# Encrypt Produced Messages

The end-to-end message encryption in Silverback is handled transparently in the producer and consumer and works independently from the used [serializer](xref:serialization) or other features like [chunking](xref:producing-chunking).

<figure>
	<a href="~/images/diagrams/encryption.png"><img src="~/images/diagrams/encryption.png"></a>
    <figcaption>The messages are transparently encrypted and decrypted.</figcaption>
</figure>

## Symmetric Encryption

Enabling the end-to-end encryption using a symmetric algorithm just require an extra configuration in the endpoint.

The default symmetric encryption algorithm is AES.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic")
                .EncryptUsingAes(encryptionKey))));
```

But you can customize it.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic")
                .Encrypt(new SymmetricEncryptionSettings))));
                {
                    AlgorithmName = "TripleDES",
                    Key = encryptionKey
                }))));
```

The <xref:Silverback.Messaging.Encryption.SymmetricEncryptionSettings> class encapsulates all common settings of a symmetric algorithm (block size, initialization vector, ...).

The `AlgorithmName` is used to load the algorithm implementation using the [SymmetricAlgorithm.Create(string)](https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.symmetricalgorithm.create) method. Refer to the [SymmetricAlgorithm](https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.symmetricalgorithm) class documentation to see which implementations are available in .net core are. Silverback uses [Aes](https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.aes) by default.

### Random Initialization Vector

If no static initialization vector is provided, a random one is automatically generated per each message and prepended to the actual encrypted message. The consumer will automatically extract and use it.

It is recommended to stick to this default behavior, for increased security.

### Key Rotation

You can smoothly rotate the key being used to encrypt the messages and to enable this feature you simply need to specify an identifier for the current key in producer endpoint configuration.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic")
                .EncryptUsingAes(encryptionKey, "key1"))));
```

## Additional Resources

* [API Reference](xref:Silverback)
* <xref:decryption> guide
