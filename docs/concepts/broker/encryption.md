---
uid: encryption
---

# Encryption

The end-to-end message encryption in Silverback is handled transparently in the producer and consumer and works independently from the used [serializer](xref:serialization) or other features like [chunking](xref:chunking).

<figure>
	<a href="~/images/diagrams/encryption.png"><img src="~/images/diagrams/encryption.png"></a>
    <figcaption>The messages are transparently encrypted and decrypted.</figcaption>
</figure>

## Symmetric encryption

Enabling the end-to-end encryption using a symmetric algorithm just require an extra configuration in the endpoint.

# [Fluent](#tab/json-fixed-type-fluent)
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
                .AddOutbound<InventoryEvent>(endpoint => endpoint
                    .ProduceTo("inventory-events")
                    .EncryptUsingAes(encryptionKey))
                .AddInbound(endpoint => endpoint
                    .ConsumeFrom("order-events")
                    .Configure(config => 
                        {
                            config.GroupId = "my-consumer";
                        })
                    .DecryptUsingAes(encryptionKey)));
}
```
# [Legacy](#tab/json-fixed-type-legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddOutbound<InventoryEvent>(
                new KafkaProducerEndpoint("inventory-events")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092"
                    },
                    Encryption = new SymmetricEncryptionSettings
                    {
                        AlgorithmName = "AES",
                        Key = encryptionKey
                    } 
                })
            .AddInbound(
                new KafkaConsumerEndpoint("order-events")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092",
                        GroupId = "my-consumer"
                    },
                    Encryption = new SymmetricEncryptionSettings
                    {
                        AlgorithmName = "AES",
                        Key = encryptionKey
                    } 
                });
}
```
***

The <xref:Silverback.Messaging.Encryption.SymmetricEncryptionSettings> class encapsulates all common settings of a symmetric algorithm (block size, initialization vector, ...).

The `AlgorithmName` is used to load the algorithm implementation using the [SymmetricAlgorithm.Create(string)](https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.symmetricalgorithm.create) method. Refer to the [SymmetricAlgorithm](https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.symmetricalgorithm) class documentation to see which implementations are available in .net core are. Silverback uses [Aes](https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.aes) by default.

### Random initialization vector

If no static initialization vector is provided, a random one is automatically generated per each message and prepended to the actual encrypted message. The consumer will automatically extract and use it.

It is recommended to stick to this default behavior, for increased security.
