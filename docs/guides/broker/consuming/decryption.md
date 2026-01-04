---
uid: decryption
---

# Decrypt Consumed Messages

End-to-end message encryption in Silverback is handled transparently in the producer and consumer and works independently from the used <xref:serialization> or other features like <xref:producing-chunking>. The consumer automatically decrypts messages if the endpoint is configured with the appropriate decryption settings.

<figure>
	<a href="~/images/diagrams/encryption.png"><img src="~/images/diagrams/encryption.png" alt="End-to-end message encryption and decryption."></a>
    <figcaption>The messages are transparently encrypted and decrypted.</figcaption>
</figure>

## Symmetric Decryption

Enabling end-to-end decryption using a symmetric algorithm requires additional configuration on the endpoint.

The default symmetric encryption algorithm is AES.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic")
                .DecryptUsingAes(decryptionKey))));
```

But you can customize it.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic")
                .Decrypt(new SymmetricDecryptionSettings
                {
                    AlgorithmName = "TripleDES",
                    Key = decryptionKey
                }))));
```

The <xref:Silverback.Messaging.Encryption.SymmetricDecryptionSettings> class contains common algorithm settings (block size, initialization vector, ...). The `AlgorithmName` value is passed to [SymmetricAlgorithm.Create(string)](https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.symmetricalgorithm.create). AES is used by default.

### Random Initialization Vector

If the producer didn't set a static initialization vector, a random IV is generated per message and prepended to the ciphertext. The consumer automatically extracts the IV and uses it for decryption. This is the recommended behavior for improved security.

### Key Rotation

To support key rotation the producer can include an identifier for the key used to encrypt each message. The consumer must be able to resolve keys by identifier to decrypt historic messages. This is done specifying a key provider function.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic")
                .DecryptUsingAes(
                    keyIdentifier => keyIdentifier switch
                    {
                        "old-key" => "...",
                        _ => "..."
                    }))));
```

(Implementation details of a key resolver depend on your integration; the important part is that the consumer can obtain the right key by the identifier included with the message.)


## Additional Resources

- [API Reference](xref:Silverback)
- <xref:encryption>
