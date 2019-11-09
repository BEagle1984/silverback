---
title: Chunking
permalink: /docs/advanced/chunking
---

The message brokers are usually very efficient at handling huge amount of relatively small messages. In order to make the most out of it you may want to split your largest messages (e.g. containing binary data) into smaller chunks. Silverback can handle such scenario transparently, reassembling the message automatically in the inbound connector before pushing it to the internal bus.

## Producer configuration

The producer endpoint can be configured to split the message into chunks by specifying their maximum size (in byte).

```c#
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

The `DbChunkStore` will temporary store the chunks into a database table and the inbound connector will rebuild the original message as soon as all chunks have been received.

**Note:** The `DbContext` must include a `DbSet<TemporaryMessageChunk>`.
{: .notice--info}

```c#
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddSilverback()
        .UseDbContext<MyDbContext>()
        .WithConnectionToKafka(options => options
            .AddDbChunkStore());
}
``` 

It is of course possible to create other implementations of `IChunkStore` to use another kind of storage for the message chunks.

