---
uid: outbox
---

# Transactional Outbox

The [transactional outbox pattern](https://microservices.io/patterns/data/transactional-outbox.html) purpose is to reliably update the database and publish the messages in the same atomic transaction. This is achieved storing the outbound messages into a temporary outbox table, whose changes are committed together with the other changes to the rest of the data.

<figure>
	<a href="~/images/diagrams/outbound-outboxtable.png"><img src="~/images/diagrams/outbound-outboxtable.png"></a>
    <figcaption>Messages 1, 2 and 3 are stored in the outbox table and produced by a separate thread or process.</figcaption>
</figure>

To implement the outbox you need to reference the storage package for the database you are using, or the one for Entity Framework if you are using it. More details can be found in the <xref:storage> guide.

The messages can be published to the outbox using the regular `IPublisher` but they will be stored in the outbox table and produced by the outbox worker.

> [!Important]
> The current <xref:Silverback.Messaging.Producing.TransactionalOutbox.OutboxWorker> cannot scale horizontally and starting multiple instances will cause the messages to be produced multiple times. The outbox worker relies therefore on the distributed locks to ensure that only one instance is running at a time.

## Configuration

A few things need to be configured to enable the outbox:
* The outbox table must be created in the database (unless using Entity Framework)
* The outbox worker must be configured to process the outbox table
* The desired endpoints must be configured to use the outbox

# [Entity Framework](#tab/ef)
```csharp
services
    .AddSilverback()
    .WithConnectionToMessageBroker(options => options
        .AddKafka()
        .AddPostgreSqlOutbox()
        .AddOutboxWorker(worker => worker
            .ProcessOutbox(outbox => outbox
                .UsePostgreSql(connectionString)))))
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic")
                .StoreToOutbox(outbox => outbox.UsePostgreSql(connectionString)))));
```
# [PostgreSQL](#tab/postgres)
```csharp
services
    .AddSilverback()
    .WithConnectionToMessageBroker(options => options
        .AddKafka()
        .AddPostgreSqlOutbox()
        .AddOutboxWorker(worker => worker
            .ProcessOutbox(outbox => outbox
                .UsePostgreSql(connectionString)))))
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic")
                .StoreToOutbox(outbox => outbox.UsePostgreSql(connectionString)))));
```
# [Sqlite](#tab/sqlite)
```csharp
services
    .AddSilverback()
    .WithConnectionToMessageBroker(options => options
        .AddKafka()
        .AddSqliteOutbox()
        .AddOutboxWorker(worker => worker
            .ProcessOutbox(outbox => outbox
                .UseSqlite(connectionString)))))
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic")
                .StoreToOutbox(outbox => outbox.UseSqlite(connectionString)))));
```
***

### Entity Framework DbContext

When using Entity Framework, the `DbContext` must be configured to include the outbox and the locks table (if used). The tables must be provisioned via migrations or by creating them manually.

```csharp
private class AppDbContext : DbContext
{
    ...

    public DbSet<SilverbackOutboxMessage> Outbox { get; set; } = null!;

    public DbSet<SilverbackLock> Locks { get; set; } = null!;
}
```

### Distributed Lock

The default locking mechanism from the selected storage package is automatically used (custom locks table for Entity Framework, advisory locks for PostgreSQL, in-memory for Sqlite, etc.), but you can customize this. The following example shows how to leverage PostgreSQL advisory locks with Entity Framework.

```csharp
.AddOutboxWorker(worker => worker
    .ProcessOutbox(outbox => outbox
        .UsePostgreSql(connectionString)
        .WithDistributedLock(distributedLock => distributedLock
            .UsePostgreSqlAdvisoryLock(connectionString)))
```

>[!Note]
> Both advisory locks and the custom locks table are implemented in the PostgreSQL storage package. The advisory locks are used by default, but you can switch to the custom locks table using `UsePostgreSqlTable`.

### Provisioning the Required Tables

The outbox table and the locks table (if used) must be created in the database. If you are using Entity Framework, you can create the tables by running the migrations. If you are using a different storage package, you can normally use the `SilverbackStorageInitializer` to create the necessary tables.

```csharp
storageInitializer.CreateSqliteOutboxAsync(connectionString);
```

## Health Check

A health check is available to monitor the outbox and alert if messages are taking too long to be produced or the queue is growing too much.

```csharp
.AddHealthChecks()
.AddOutboxCheck(
    maxAge: TimeSpan.FromSeconds(30),
    maxQueueLength: 100)
```

## Additional Resources

* [API Reference](xref:Silverback)
* <xref:storage> guide
