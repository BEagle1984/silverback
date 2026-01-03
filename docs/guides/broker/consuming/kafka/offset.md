---
uid: kafka-offset
---

# Kafka Offset Storage

Silverback can store Kafka consumer offsets **outside of Kafka**, using its storage layer (in-memory or a database). This is sometimes referred to as *client-side offset storage*.

This feature is primarily meant to enable **exactly-once processing** *with respect to your own database*, by persisting:

* your business changes (e.g., inserts/updates), and
* the “I have processed up to offset X” marker

in the **same database transaction**.

If the transaction commits, both the data and the new offset are persisted. If it rolls back, neither is persisted and the messages will be re-consumed.

> [!Important]
> Client-side offset storage is not a Kafka feature. It’s an application pattern.
> Silverback deliberately **disables committing offsets to Kafka** and repositions the consumer based on the offsets stored in your chosen storage.

## When to use it

Use client-side offset storage when your application:
* processes incoming messages and writes to a database
* needs deterministic recovery after crashes/restarts
* wants to avoid the classic “processed but offset not committed” / “offset committed but processing rolled back” failure modes

This pairs well with the <xref:outbox> if your consuming flow produces new messages as part of the same unit of work.

## How it works (high level)

1. As messages are processed, Silverback tracks the offsets that are safe to mark as processed. 
2. When offsets are stored, they are written to the configured store.
3. When the consumer reconnects (or partitions are assigned), Silverback loads the stored offsets and repositions the consumer accordingly.

## Configuration

The configuration has two parts:

1. Register the offset store implementation (storage package)
2. Enable client-side offset storage on the consumer

All examples below intentionally call `.DisableOffsetsCommit()` so that Kafka broker offsets aren’t used.

### In-memory offset store

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

### SQLite offset store

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

### PostgreSQL offset store

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

### Entity Framework offset store

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

## Exactly-once semantics with transactions

To make offset writes **atomic** with your business changes, enlist the database transaction in the offset store.

Silverback exposes a `KafkaOffsetStoreScope` that you can inject into your subscriber. You then:

1. Start a transaction for your business work
2. Enlist that transaction in the scope
3. Perform your business work
4. Store offsets
5. Commit

If the transaction rolls back, offsets won’t be stored and the messages will be re-consumed.

### A minimal example

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

### Entity Framework transaction example

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

## Batch processing

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

## Provisioning the required tables

Depending on the chosen storage:

* **Entity Framework**: create the table via migrations (because it’s part of your `DbContext`).
* **Relational storage packages (SQLite/PostgreSQL, etc.)**: you can create the table using `SilverbackStorageInitializer`.

Examples:

```csharp
await storageInitializer.CreateSqliteKafkaOffsetStoreAsync(connectionString);
// or
await storageInitializer.CreatePostgreSqlKafkaOffsetStoreAsync(connectionString);
```

## Gotchas and best practices

* **Always disable Kafka offset commits** when using client-side offset storage: `DisableOffsetsCommit()`.
* Keep the same **GroupId** and topic naming; offsets are keyed by consumer group and partition.
* Use **a durable store** (SQLite/PostgreSQL/EF) in production.
* For “exactly-once w.r.t. DB”, always **enlist the same transaction** used for your business changes.
* Consider pairing this with <xref:outbox> if you also publish messages during processing.

## Additional resources

* API Reference (`docs/api/`)
* <xref:storage> guide
* <xref:outbox> guide
