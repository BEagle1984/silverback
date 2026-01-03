---
uid: storage
---

# Storage Integration

Silverback's *storage layer* provides a set of **optional persistence components** that enable features which need a durable, shared state outside the message broker.

Today, storage is used primarily for:

* **Transactional Outbox** (reliably publish messages after a DB transaction commits)
* **Kafka client-side offset storage** (store consumer offsets in your DB for exactly-once *w.r.t.* your database)
* **Distributed locking** (ensure that only one node/instance performs a critical background task)

This guide explains the purpose of the storage layer, what implementations exist, how to initialize/provision the required tables, how to use the features at a high level, and how distributed locking works.

> [!Note]
> Storage is intentionally modular.
> You reference and configure *only* the storage packages you need.

## What packages exist

At the moment, Silverback ships the following storage-related packages:

### In-memory (development/testing)

Package: `Silverback.Storage.Memory`

Provides:

* In-memory transactional outbox (volatile)
* In-memory Kafka offset store (volatile)
* In-memory lock (not truly distributed)

Use it for unit tests, local development, or demos. If the process stops, all data is lost.

### SQLite (development/testing)

Package: `Silverback.Storage.Sqlite`

Provides:

* SQLite outbox table + reader/writer
* SQLite Kafka offset store table
* Uses the in-memory lock by default (intended for testing)

> [!Important]
> SQLite is great for local/dev scenarios.
> If you need *horizontal scalability*, SQLite is usually not the right choice.

### PostgreSQL

Package: `Silverback.Storage.PostgreSql`

Provides:

* PostgreSQL outbox table + reader/writer
* PostgreSQL Kafka offset store table
* Two distributed lock implementations:
  * PostgreSQL **advisory locks** (default and recommended)
  * PostgreSQL **table-based lock** (locks table with heartbeat)

### Entity Framework (when you already use EF Core)

Package: `Silverback.Storage.EntityFramework`

Provides:

* EF Core outbox reader/writer (stores messages in your `DbContext`)
* EF Core Kafka offset store (stores offsets in your `DbContext`)
* EF Core table-based distributed lock (locks table with heartbeat)

Because EF storage uses your application's model, tables are typically created via **migrations**.

### Relational abstraction (internal building block)

Package: `Silverback.Storage.Relational`

This package contains shared building blocks for relational stores (e.g., the base implementation of table-based locks). Most applications don't need to reference it directly.

## How to initialize / provision the storage

Some storage features require database tables.

How these tables are created depends on the chosen provider:

* **Entity Framework**: tables are part of your `DbContext` and are typically created via migrations.
* **Relational storage packages** (SQLite/PostgreSQL): you can create tables programmatically using `SilverbackStorageInitializer`.

### Using `SilverbackStorageInitializer`

The initializer is a tiny helper type you can instantiate from your `IServiceProvider` and then call provider-specific extension methods.

Example (conceptual):

```csharp
await using var services = serviceCollection.BuildServiceProvider();
var initializer = new SilverbackStorageInitializer(services);

await initializer.CreateSqliteOutboxAsync(connectionString);
await initializer.CreateSqliteKafkaOffsetStoreAsync(connectionString);
```

Provider-specific provisioning methods currently include (non-exhaustive):

* SQLite:
  * `CreateSqliteOutboxAsync`
  * `CreateSqliteKafkaOffsetStoreAsync`
* PostgreSQL:
  * `CreatePostgreSqlOutboxAsync`
  * `CreatePostgreSqlKafkaOffsetStoreAsync`
  * `CreatePostgreSqlLocksTableAsync` (only needed for table-based locks)

> [!Note]
> Advisory locks do **not** require a table.
>
> Table-based locks (PostgreSQL table lock + EF lock) do require a locks table.

### Entity Framework tables

When using EF Core, you include the storage entities in your `DbContext`.

Outbox:

```csharp
public DbSet<SilverbackOutboxMessage> Outbox { get; set; } = null!;
```

Locks (if used):

```csharp
public DbSet<SilverbackLock> Locks { get; set; } = null!;
```

Kafka offsets:

```csharp
public DbSet<SilverbackStoredOffset> KafkaOffsets { get; set; } = null!;
```

Then create the tables using EF migrations.

## How to use storage features (high level)

Storage is usually not used directly.
Instead you:

1. Reference the storage package
2. Register it in Silverback via `WithConnectionToMessageBroker(...).AddXyz...`
3. Enable the feature (outbox worker / offset store / distributed lock usage)

### Transactional Outbox

The transactional outbox stores outbound messages in a local persistence store and lets a background worker publish them.

High level steps:

1. Configure an outbox implementation (EF/SQLite/PostgreSQL)
2. Configure the outbox worker to process the outbox
3. Configure producers/endpoints to store produced messages in the outbox
4. Enlist the same DB transaction in the publisher (recommended)

See the dedicated guide:

* <xref:outbox>

### Kafka offset storage (client-side offsets)

Client-side offset storage persists offsets outside Kafka so that your processing + offset advancement can be committed atomically in your DB transaction.

High level steps:

1. Configure an offset store implementation (Memory/SQLite/PostgreSQL/EF)
2. Disable committing offsets to Kafka (`DisableOffsetsCommit()`)
3. Enable client-side offset storage on your consumer
4. Enlist the same DB transaction in the `KafkaOffsetStoreScope` (recommended)

See the dedicated guide:

* <xref:kafka-offset>

## Distributed locking

Some features (most notably the outbox worker) must ensure that only **one** instance is active across a cluster.
Silverback achieves this by acquiring a lock before running and by monitoring lock ownership.

Depending on the storage package you use, the default lock implementation may differ:

* In-memory storage uses `InMemoryLock` (not distributed)
* PostgreSQL storage supports advisory locks (default) and table-based locks
* Entity Framework storage uses a table-based lock

### Why locks exist

Distributed locks are used to avoid scenarios like:

* two instances producing outbox messages concurrently (leading to duplicates)
* multiple nodes running a single-instance maintenance task

### Lock handle and lock loss

When a lock is acquired, Silverback obtains a `DistributedLockHandle`.

Key behaviors:

* The handle is **disposable**. Disposing it releases the lock.
* The handle exposes a `LockLostToken`.
  * Continuous background work can observe this token and stop if the lock is lost.
  * For advisory locks, this token is signaled if the underlying database lock/session is lost.
  * For heartbeat-based locks, this token is signaled if a heartbeat update fails.

### PostgreSQL advisory lock

Implementation: `PostgreSqlAdvisoryLock` (uses `DistributedLock.Postgres` a.k.a. Medallion.Threading).

Characteristics:

* No locks table required.
* Strong choice for clustered deployments.
* Lock is held for the lifetime of the DB session/handle.
* `LockLostToken` is provided by the underlying advisory lock handle.

You can configure advisory locks explicitly where a distributed lock is configurable (for example in the outbox worker):

```csharp
.WithDistributedLock(lockSettings => lockSettings
    .UsePostgreSqlAdvisoryLock("outbox", connectionString))
```

### Table-based lock (PostgreSQL and EF)

Base implementation: `TableBasedDistributedLock`.

How it works:

1. A process tries to acquire the lock by writing/updating a row for `LockName` and claiming it with a unique `Handler` id.
2. While the lock is held, a background heartbeat periodically updates the lock row.
3. If heartbeats stop (crash, network issues), the lock becomes eligible to be re-acquired after `LockTimeout`.

Settings to know (defaults shown):

* `AcquireInterval` (default 1s): delay between acquisition attempts.
* `HeartbeatInterval` (default 500ms): how often the owner writes heartbeats.
* `LockTimeout` (default 5s): if `LastHeartbeat` is older than this, the lock is considered stale and can be taken over.

> [!Important]
> With table-based locks, *clock skew* across nodes can cause surprising behavior.
> Prefer a consistent time source and keep nodes time-synchronized.

PostgreSQL table lock specifics:

* Implementation: `PostgreSqlTableLock`
* Requires a locks table (create via `CreatePostgreSqlLocksTableAsync`)

Entity Framework lock specifics:

* Implementation: `EntityFrameworkLock`
* Uses a `DbContext` and the `SilverbackLock` entity/table.
* Handles acquisition races by catching `DbUpdateException` (another process won).

### In-memory lock

Implementation: `InMemoryLock`.

* Uses a local `SemaphoreSlim`.
* Not distributed: it only protects code paths within the same process.
* `LockLostToken` is `CancellationToken.None`.

## Putting it together: which storage should I pick?

A practical rule of thumb:

* **Unit tests / local quick start**: `Silverback.Storage.Memory`
* **Local integration tests**: `Silverback.Storage.Sqlite`
* **Production with PostgreSQL**: `Silverback.Storage.PostgreSql` (advisory locks recommended)
* **Production with EF Core and a relational database**: `Silverback.Storage.EntityFramework`

## Additional Resources

* [API Reference](xref:Silverback)
* <xref:outbox> guide
* <xref:kafka-offset> guide
