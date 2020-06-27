---
uid: chunking
---

# Chunking

The message brokers are usually very efficient at handling huge amount of relatively small messages. In order to make the most out of it you may want to split your largest messages (e.g. containing binary data) into smaller chunks. Silverback can handle such scenario transparently, reassembling the message automatically in the inbound connector before pushing it to the internal bus.

## Producer configuration

The producer endpoint can be configured to split the message into chunks by specifying their maximum size (in bytes).

<figure>
	<a href="~/images/diagrams/chunk-basic.png"><img src="~/images/diagrams/chunk-basic.png"></a>
    <figcaption>The messages are being split into small chunks.</figcaption>
</figure>

```csharp
new KafkaProducerEndpoint("silverback-examples-events")
{
    Configuration = new KafkaProducerConfig
    {
        ...
    },
    Chunk = new ChunkSettings
    {
        Size = 100000
    }
}
```

## Consumer configuration

An `IChunkStore` implementation is needed to temporary store the chunks until the full message has been received. Silverback has two built-in implementations: the `InMemoryChunkStore` and the `DbChunkStore`.

<figure>
	<a href="~/images/diagrams/chunk-basic.png"><img src="~/images/diagrams/chunk-basic.png"></a>
    <figcaption>The message chunks are automatically aggregated once the full message is received.</figcaption>
</figure>

### In-Memory temporary store

The `InMemoryChunkStore` should be your default choice as it doesn't require any extra storage. As the name suggests, the chunks are simply kept in memory until the full message can be rebuilt and consumed.

This approach has of course some limitations and requires that all chunks are received by the same consumer: with Kafka this is usually ensured by setting the same key to all chunks, thing that is done automatically by Silverback setting the same key and the same headers to all chunks (except for the chunk index header, of course).

<figure>
	<a href="~/images/diagrams/chunk-nokey.png"><img src="~/images/diagrams/chunk-nokey.png"></a>
    <figcaption>Kafka: without a key the messages are written to a random partition.</figcaption>
</figure>

<figure>
	<a href="~/images/diagrams/chunk-key.png"><img src="~/images/diagrams/chunk-key.png"></a>
    <figcaption>Kafka: with the key being set the chunks related to the same message will land in the same partition.</figcaption>
</figure>

On the other hand it isn't a big deal if multiple producers are writing in the same topic/queue and the chunks of different message are interleaved. Silverback will commit the offsets only when no chunk is left in memory, to prevent any message loss.

<figure>
	<a href="~/images/diagrams/chunk-interleaved.png"><img src="~/images/diagrams/chunk-interleaved.png"></a>
    <figcaption>Silverback handles the inteleaved chunks transparently.</figcaption>
</figure>


> [!Important]
> It isn't guaranteed that you will never consume a message twice, if you use multiple producers to write into the topic or queue. It is up to you to ensure idempotency or enable exactly-once processing through the inbound connector (see <xref:inbound>).

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .UseDbContext<MyDbContext>()
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddInMemoryChunkStore());
    }
}
```

> [!Tip]
> The `AddInMemoryChunkStore` method has some optional parameters to configure the maximum chunks retention (in case of a producer failure, you may receive only part of the chunks of a given message and without this setting they would stay in memory forever).

### Database temporary store

The `DbChunkStore` will temporary store the chunks into a database table and the inbound connector will rebuild the original message as soon as all chunks have been received. This table can be shared between multiple consumers, making it safer when chunks aren't properly written to the same partition.

<figure>
	<a href="~/images/diagrams/chunk-persisted.png"><img src="~/images/diagrams/chunk-persisted.png"></a>
    <figcaption>The chunks are persisted to a database table that is shared across the consumers.</figcaption>
</figure>

> [!Note]
> The `Silverback.EntityFrameworkCore` package is also required and the `DbContext` must include a `DbSet<TemporaryMessageChunk>`. See also the <xref:dbcontext>.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .UseDbContext<MyDbContext>()
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddDbChunkStore());
    }
}
```

> [!Tip]
> The `AddDbChunkStore` method has some optional parameters to configure the maximum chunks retention (in case of a producer failure, you may receive only part of the chunks of a given message and without this setting they would stay in the database forever).

### Custom chunk store

It is of course possible to create other implementations of `IChunkStore` to use another kind of storage for the message chunks.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .UseDbContext<MyDbContext>()
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddChunkStore<SomeCustomChunkStore>());
    }
}
```
