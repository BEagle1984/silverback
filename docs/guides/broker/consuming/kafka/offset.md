---
uid: kafka-offset
description: "Store Kafka consumer offsets in your own database with Silverback client-side offset storage for exactly-once processing with respect to your data."
---

# Kafka Offset Storage

Silverback can store Kafka consumer offsets **outside of Kafka**, using its storage layer (in-memory or a database). This is sometimes referred to as *client-side offset storage*.

This feature is primarily meant to enable **exactly-once processing** *with respect to your own database*, by persisting:

* your business changes (e.g., inserts/updates), and
* the “I have processed up to offset X” marker

in the **same database transaction**.

If the transaction commits, both the data and the new offset are persisted. If it rolls back, neither is persisted and the messages will be re-consumed, unless the [error policies](xref:error-policies) skip or move them: their offsets are then stored outside the failed transaction, so that they aren't skipped or moved again. The offsets of the messages that are discarded without reaching the subscribers (e.g., a duplicated chunk or the chunks of an incomplete chunked message) are stored as well, unless a batch or another sequence is still pending: they're then stored with the next commit, not to skip the pending messages after a restart.

Like the Kafka commit, an offset lower than the one already committed by the consumer isn't stored (e.g., the offset of an incomplete chunked message aborted after the next messages were committed), also when the Kafka offsets commit is disabled, unless the partition was sought via <xref:Silverback.Messaging.Broker.IKafkaConsumer.Seek(Confluent.Kafka.TopicPartitionOffset)> since it was assigned, and neither are the offsets of a partition being rolled back. If the partition is revoked while a message is being processed, its offset is stored anyway when the message is committed (atomically with your changes, if stored in your transaction), so that the message isn't processed again after the partition is reassigned. The offsets of the skipped or moved messages of a revoked partition aren't stored instead, so these messages are consumed again by the next owner of the partition.

> [!Note]
> If the offsets of a skipped or moved message cannot be stored, the store is retried once and then the error is logged (`SkippedOffsetsStoreError`, see [Logging](xref:logging)). The message is skipped or moved anyway and the stored offsets are updated by the next processed message. If the consumer is restarted (or the partition is reassigned) before, the message is consumed and skipped or moved again.

> [!Note]
> Since the offsets of the skipped or moved messages are stored outside any transaction, their store can complete after a concurrent store of a higher offset, e.g., when an incomplete chunked message is aborted by its sequence timeout while the next messages are being committed. The stored offset then moves back below the one stored by the concurrent commit until the next message is committed, and some messages may be consumed again after a restart or rebalance (also when the Kafka offsets commit is enabled, since the consumer is repositioned based on the stored offsets). In this case the messages are delivered at least once, so design your subscribers to be idempotent.

> [!Important]
> Client-side offset storage is not a Kafka feature. It’s an application pattern.
> Silverback deliberately repositions the consumer based on the offsets stored in your chosen storage, instead of relying on the server-side Kafka offsets.
> You can even disable storing the offsets in the Kafka broker using `DisableOffsetsCommit()`.

## When to Use It

Use client-side offset storage when your application:
* processes incoming messages and writes to a database
* needs deterministic recovery after crashes/restarts
* wants to avoid the classic “processed but offset not committed” / “offset committed but processing rolled back” failure modes

This pairs well with the <xref:outbox> if your consuming flow produces new messages as part of the same unit of work.

## How It Works (High Level)

1. As messages are processed, Silverback tracks the offsets that are safe to mark as processed. 
2. When offsets are stored, they are written to the configured store.
3. When the consumer reconnects (or partitions are assigned), Silverback loads the stored offsets and repositions the consumer accordingly.

## Configuration

The configuration has two parts:

1. Register the offset store implementation (storage package)
2. Enable client-side offset storage on the consumer

All examples below intentionally call `.DisableOffsetsCommit()` so that Kafka broker offsets aren’t used.

### In-Memory Offset Store

Use this for development/testing only (offsets are lost on process restart).

```csharp
services
    .AddSilverback()
    .WithConnectionToMessageBroker(options => options
        .AddKafka()
        .AddInMemoryKafkaOffsetStore())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer(consumer => consumer
            .WithGroupId("my-group")
            .DisableOffsetsCommit()
            .StoreOffsetsClientSide(offsetStore => offsetStore.UseMemory())
            .Consume(endpoint => endpoint
                .ConsumeFrom("my-topic"))));
```

### SQLite Offset Store

```csharp
services
    .AddSilverback()
    .WithConnectionToMessageBroker(options => options
        .AddKafka()
        .AddSqliteKafkaOffsetStore())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer(consumer => consumer
            .WithGroupId("my-group")
            .DisableOffsetsCommit()
            .StoreOffsetsClientSide(offsetStore => offsetStore.UseSqlite(connectionString))
            .Consume(endpoint => endpoint
                .ConsumeFrom("my-topic"))));
```

### PostgreSQL Offset Store

```csharp
services
    .AddSilverback()
    .WithConnectionToMessageBroker(options => options
        .AddKafka()
        .AddPostgreSqlKafkaOffsetStore())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer(consumer => consumer
            .WithGroupId("my-group")
            .DisableOffsetsCommit()
            .StoreOffsetsClientSide(offsetStore => offsetStore.UsePostgreSql(connectionString))
            .Consume(endpoint => endpoint
                .ConsumeFrom("my-topic"))));
```

### Entity Framework Offset Store

When using Entity Framework, offsets are stored using a `DbContext`.

```csharp
services
    .AddDbContextFactory<AppDbContext>(options => options.UseNpgsql(connectionString))
    .AddSilverback()
    .WithConnectionToMessageBroker(options => options
        .AddKafka()
        .AddEntityFrameworkKafkaOffsetStore())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer(consumer => consumer
            .WithGroupId("my-group")
            .DisableOffsetsCommit()
            .StoreOffsetsClientSide(offsetStore => offsetStore.UseEntityFramework<AppDbContext>())
            .Consume(endpoint => endpoint
                .ConsumeFrom("my-topic"))));

public class AppDbContext : DbContext
{
    // ...your entities...

    public DbSet<SilverbackStoredOffset> KafkaOffsets { get; set; } = null!;
}
```

> [!Note]
> For more details about storage packages and setup, see the <xref:storage> guide.

## Exactly-Once Semantics with Transactions

To make offset writes **atomic** with your business changes, enlist the database transaction in the offset store.

Silverback exposes a `KafkaOffsetStoreScope` that you can inject into your subscriber. You then:

1. Start a transaction for your business work
2. Enlist that transaction in the scope
3. Perform your business work
4. Store offsets
5. Commit

If the transaction rolls back, offsets won’t be stored and the messages will be re-consumed.

### A Minimal Example

```csharp
public class MySubscriber
{
    private readonly DbConnection _connection;

    public MySubscriber(DbConnection connection) => _connection = connection;

    public async Task OnMessageReceivedAsync(MyMessage message, KafkaOffsetStoreScope offsetStoreScope)
    {
        await _connection.OpenAsync();

        await using (DbTransaction tx = await _connection.BeginTransactionAsync())
        {
            // Make offset persistence part of the same transaction
            offsetStoreScope.EnlistTransaction(tx);

            // 1) Your business changes
            await SaveBusinessChangesAsync(message, tx);

            // 2) Mark offsets as processed (persist them)
            await offsetStoreScope.StoreOffsetsAsync();

            await tx.CommitAsync();
        }
    }

    private static Task SaveBusinessChangesAsync(MyMessage message, DbTransaction tx)
    {
        // ...insert/update your database rows here...
        return Task.CompletedTask;
    }
}
```

> [!Important]
> Calling `StoreOffsetsAsync()` without enlisting a transaction still stores offsets, but you lose the atomic “exactly-once w.r.t. DB” guarantee.

### Entity Framework Transaction Example

```csharp
public async Task OnMessageReceivedAsync(MyMessage message, KafkaOffsetStoreScope offsetStoreScope, AppDbContext db)
{
    await using var tx = await db.Database.BeginTransactionAsync();

    offsetStoreScope.EnlistTransaction(tx.GetDbTransaction());

    // ...db.Add / db.Update ...
    await db.SaveChangesAsync();

    await offsetStoreScope.StoreOffsetsAsync();

    await tx.CommitAsync();
}
```

## Batch Processing

If batch processing is enabled, offsets are tracked for the entire batch. Storing offsets marks the whole batch as processed.

```csharp
.AddConsumer(consumer => consumer
    .WithGroupId("my-group")
    .DisableOffsetsCommit()
    .StoreOffsetsClientSide(offsetStore => offsetStore.UsePostgreSql(connectionString))
    .Consume(endpoint => endpoint
        .ConsumeFrom("my-topic")
        .EnableBatchProcessing(100)))
```

> [!Note]
> In batch mode, a failure after processing items 1..N but before storing offsets can lead to reprocessing of part (or all) of the batch after restart. Design your handler code to be idempotent.

## Provisioning the Required Tables

Depending on the chosen storage:

* **Entity Framework**: create the table via migrations (because it’s part of your `DbContext`).
* **Relational storage packages (SQLite/PostgreSQL, etc.)**: you can create the table using `SilverbackStorageInitializer`.

Examples:

```csharp
await storageInitializer.CreateSqliteKafkaOffsetStoreAsync(connectionString);
// or
await storageInitializer.CreatePostgreSqlKafkaOffsetStoreAsync(connectionString);
```

## Gotchas and Best Practices

* **Always disable Kafka offset commits** when using client-side offset storage: `DisableOffsetsCommit()`.
* Keep the same **GroupId** and topic naming; offsets are keyed by consumer group and partition.
* Use **a durable store** (SQLite/PostgreSQL/EF) in production.
* For “exactly-once w.r.t. DB”, always **enlist the same transaction** used for your business changes.
* Consider pairing this with <xref:outbox> if you also publish messages during processing.

## Additional Resources

- [API Reference](xref:Silverback)
- <xref:storage>
- <xref:outbox>
