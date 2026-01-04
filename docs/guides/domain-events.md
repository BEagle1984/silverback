---
uid: domain-events
---

# Domain-Driven Design and Domain Events

Domain events are a great way to model important business facts in a Domain-Driven Design (DDD) style.
They let your aggregate record *what happened* (e.g. "order placed", "basket checked out") and then publish those events so that other parts of the application can react.

Silverback provides built-in support to collect domain events from your entities and publish them through the internal message bus.
When used together with storage and the transactional outbox, this becomes a solid foundation for reliable integration events.

## What Silverback considers a "domain event"

A domain event is any message implementing <xref:Silverback.Domain.IDomainEvent>.

The recommended approach is to use [Silverback.Core.Model](https://www.nuget.org/packages/Silverback.Core.Model/) and derive your events from <xref:Silverback.Domain.DomainEvent`1>.

## Storing domain events in your entities

Silverback can publish domain events that are stored inside the entities being saved.
To make this work, your entities must implement <xref:Silverback.Messaging.Messages.IMessagesSource>.

The easiest way is to inherit from <xref:Silverback.Domain.DomainEntity>, which already implements the required interface and exposes a convenient `AddEvent<TEvent>()` method.

> [!Tip]
> You don't have to use `DomainEntity`. If you have your own base entity type, you can still integrate with this feature by implementing <xref:Silverback.Messaging.Messages.IMessagesSource>.

### Sample aggregate root

In this example, calling `Checkout()` records a domain event by adding it to the in-memory event collection.

```csharp
using Silverback.Domain;

namespace Sample;

public class Basket : DomainEntity
{
    private Basket()
    {
        // Required by EF Core
    }

    public Basket(Guid userId)
    {
        UserId = userId;
        Created = DateTime.UtcNow;
    }

    public int Id { get; private set; }

    public Guid UserId { get; private set; }

    public DateTime Created { get; private set; }

    public DateTime? CheckoutDate { get; private set; }

    public void Checkout()
    {
        CheckoutDate = DateTime.UtcNow;

        // Records the domain event to be published as part of SaveChanges.
        AddEvent<BasketCheckedOutDomainEvent>();
    }
}

public sealed class BasketCheckedOutDomainEvent : DomainEvent<Basket>;
```

> [!Note]
> `DomainEntity` also exposes a `DomainEvents` property. It's primarily there for inspection/testing, while publication is handled by the publisher described below.

## Publishing domain events with Entity Framework Core

To publish domain events as part of your `DbContext.SaveChanges` call, use <xref:Silverback.Domain.EntityFrameworkDomainEventsPublisher`1>.

This helper is designed to:

- scan the EF Core change tracker for entities implementing `IMessagesSource`
- publish the collected domain events via <xref:Silverback.Messaging.Publishing.IPublisher>
- align the publisher storage transaction (used e.g. by the transactional outbox) with the current EF Core transaction when possible

### DbContext integration

Create an `EntityFrameworkDomainEventsPublisher<TDbContext>` instance and delegate the `SaveChanges*` calls to it.

```csharp
using Microsoft.EntityFrameworkCore;
using Silverback.Domain;
using Silverback.Messaging.Publishing;

namespace Sample;

public class SampleDbContext : DbContext
{
    private readonly EntityFrameworkDomainEventsPublisher<SampleDbContext>? _domainEventsPublisher;

    public SampleDbContext(DbContextOptions<SampleDbContext> options, IPublisher publisher)
        : base(options)
    {
        _domainEventsPublisher =
            new EntityFrameworkDomainEventsPublisher<SampleDbContext>(
                this,
                base.SaveChanges,
                base.SaveChangesAsync,
                publisher);
    }

    public DbSet<Basket> Baskets => Set<Basket>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        _domainEventsPublisher?.SaveChangesAndPublishDomainEvents(acceptAllChangesOnSuccess) ??
        base.SaveChanges(acceptAllChangesOnSuccess);

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default) =>
        _domainEventsPublisher?.SaveChangesAndPublishDomainEventsAsync(acceptAllChangesOnSuccess, cancellationToken) ??
        base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
}
```

> [!Important]
> The example above wires domain event publication directly into `SaveChanges`. This makes it hard to "forget" publishing and keeps your service code clean.

## Handling published domain events

Domain events are published to the internal bus, so you can handle them like any other in-memory message (see <xref:bus>).

A typical subscriber method might look like this:

```csharp
using Silverback.Domain;

namespace Sample;

public class BasketDomainEventsSubscriber
{
    public Task OnBasketCheckedOut(BasketCheckedOutDomainEvent domainEvent)
    {
        // TODO: react to the event (send emails, update projections, etc.)
        return Task.CompletedTask;
    }
}
```

Register the subscriber using `AddScopedSubscriber`, `AddSingletonSubscriber`, or `AddTransientSubscriber`.

## Domain events vs integration events

Domain events are usually *internal* to a bounded context.
If you want to notify other services via Kafka/MQTT/etc., you typically convert domain events into integration events.

If you're using the transactional outbox, you can do that conversion in a subscriber and publish an integration message via the same <xref:Silverback.Messaging.Publishing.IPublisher>.

> [!Tip]
> If you're building an event-driven architecture, combining domain events + outbox is a common way to keep your database changes and outgoing messages consistent.

## Common pitfalls

- **Publishing from inside the entity**: the entity should only *record* events (`AddEvent<T>()`). Publishing is infrastructure.
- **Forgetting to hook into SaveChanges**: if you don't integrate `EntityFrameworkDomainEventsPublisher`, recorded events will never be published.
- **Long-running subscribers**: domain events run in-process; keep handlers quick and delegate slow work to background jobs or integration messages.

## Next steps

- Learn how to publish and subscribe using the internal bus: <xref:bus>
- Learn how to connect to Kafka/MQTT and produce externally: <xref:setup>
