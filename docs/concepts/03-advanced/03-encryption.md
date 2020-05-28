---
uid: encryption
---

# Encryption

The end-to-end message encryption in Silverback is handled transparently in the produce/consume pipeline and works independently from the used [serializer](xref:serialization) or other features like [chunking](xref:chunking).

<figure>
	<a href="~/images/diagrams/encryption.png"><img src="~/images/diagrams/encryption.png"></a>
    <figcaption>The messages are transparently encrypted and decrypted.</figcaption>
</figure>

## Symmetric encryption

Enabling the end-to-end encryption using a symmetric algorithm just require an extra configuration in the endpoint (a `KafkaConsumerEndpoint` is used in the example below but the `Encryption` property is available in all enpoint types).

```csharp
new KafkaConsumerEndpoint("sensitive-data")
{
    Configuration = new KafkaProducerConfig
    {
        BootstrapServers = "PLAINTEXT://kafka:9092"
    },
    Encryption = new SymmetricEncryptionSettings
    {
        //AlgorithmName = "AES", (AES is the default algorithm)
        Key = encryptionKey
    }
}
```

The `SymmetricEncryptionSettings` class exposes all common properties of a symmetric algorithm (block size, initialization vector, ...).

The `AlgorithmName` is used to load the algorithm implementation using the `System.Security.Cryptography.SymmetricAlgorithm.Create(string algName)` method. Some default algorithms available in .net core are: `"AES"` (default in Silverback), `"TripleDes"` and `"Rijndael"`.

### Random initialization vector

If no static initialization vector is provided, a random one is automatically generated per each message and prepended to the actual encrypted message. The consumer will automatically extract and use it.

It is recommended to stick to this default behavior, for increased security.