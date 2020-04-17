---
title: Chunking
permalink: /docs/advanced/chunking
---

The message brokers are usually very efficient at handling huge amount of relatively small messages. In order to make the most out of it you may want to split your largest messages (e.g. containing binary data) into smaller chunks. Silverback can handle such scenario transparently, reassembling the message automatically in the inbound connector before pushing it to the internal bus.

## Producer configuration

The producer endpoint can be configured to split the message into chunks by specifying their maximum size (in byte).

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

### In-Memory temporary store

The `InMemoryChunkStore` should be your default choice as it doesn't require any extra storage. As the name suggests, the chunks are simply kept in memory until the full message can be rebuilt and consumed.

This approach has of course some limitations and requires that all chunks are received by the same consumer: with Kafka this is usually ensured by setting the same key to all chunks, thing that is done automatically by Silverback setting the same key and the same headers to all chunks (except for the chunk index header, of couse).

On the other hand it isn't a big deal if multiple producers are writing in the same topic/queue and the chunks of different message are interleaved. Silverback will commit the offsets only when no chunk is left in memory, to prevent any message loss.

It isn't guaranteed that you will never consume a message twice, if you use multiple producers to write into the topic or queue. It is up to you to ensure idempotency or enable exactly-once processing through the inbound connector (see [Inbound Connector]({{ site.baseurl }}/docs/configuration/inbound)).
{.notice--important}

<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
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
{% endhighlight %}
</figure>

The `AddInMemoryChunkStore` method has some optional parameters to configure the maximum chunks retention (in case of a producer failure, you may receive only part of the chunks of a given message and without this setting they would stay in memory forever).
{.notice--info}

### Database temporary store

The `DbChunkStore` will temporary store the chunks into a database table and the inbound connector will rebuild the original message as soon as all chunks have been received.

The `DbContext` must include a `DbSet<TemporaryMessageChunk>`.
{: .notice--note}

<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
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
{% endhighlight %}
</figure>

The `AddDbChunkStore` method has some optional parameters to configure the maximum chunks retention (in case of a producer failure, you may receive only part of the chunks of a given message and without this setting they would stay in the database forever).
{.notice--info}

### Custom chunk store

It is of course possible to create other implementations of `IChunkStore` to use another kind of storage for the message chunks.

<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
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
{% endhighlight %}
</figure>

