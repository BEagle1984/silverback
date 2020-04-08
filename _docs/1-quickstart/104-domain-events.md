---
title: DDD and Domain Events
permalink: /docs/quickstart/ddd
toc: false
---

One of the core features of Silverback is the ability to publish the domain events as part of the `DbContext` save changes transaction in order to guarantee consistency.

The `Silverback.Core.Model` package contains a sample implementation of a `DomainEntity` but you can also implement you own type. 

In case of a custom implementation the only constraint is that you must implement the `IMessagesSource` interface in order for Silverback to be able to access the associated events.
{: .notice--important}

```csharp
using Silverback.Domain;

namespace Sample
{
    public class Basket : DomainEntity, IAggregateRoot
    {
        private readonly List<BasketItem> _items = new List<BasketItem>();

        private Basket()
        {
        }

        public Basket(Guid userId)
        {
            UserId = userId;
            Created = DateTime.UtcNow;
        }

        [Key]
        public int Id { get; private set; }
        public IEnumerable<BasketItem> Items => _items.AsReadOnly();
        public Guid UserId { get; private set; }
        public DateTime Created { get; private set; }
        public DateTime? CheckoutDate { get; private set; }

        public void Checkout()
        {
            CheckoutDate = DateTime.UtcNow;

            AddEvent<BasketCheckoutEvent>();
        }
    }
}
```

The `AddEvent<TEvent>()` method adds the domain event to the events collection, to be published when the entity is saved.

To enable this mechanism we just need to override the various `SaveChanges` methods to plug-in the `DbContextEventsPublisher` contained in the `Silverback.Core.EntityFrameworkCore` package.

```csharp
public class MyDbContext : DbContext
{
    private readonly DbContextEventsPublisher _eventsPublisher;

    public MyDbContext(IPublisher publisher)
    {
        _eventsPublisher = new DbContextEventsPublisher(publisher, this);
    }

    public MyDbContext(DbContextOptions options, IPublisher publisher)
        : base(options)
    {
        _eventsPublisher = new DbContextEventsPublisher(publisher, this);
    }

    public override int SaveChanges()
        => SaveChanges(true);

    public override int SaveChanges(
        bool acceptAllChangesOnSuccess)
        => _eventsPublisher.ExecuteSaveTransaction(() => 
            base.SaveChanges(acceptAllChangesOnSuccess));

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
        => SaveChangesAsync(true, cancellationToken);

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, 
        CancellationToken cancellationToken = default)
        => _eventsPublisher.ExecuteSaveTransactionAsync(() =>
            base.SaveChangesAsync(
                acceptAllChangesOnSuccess, 
                cancellationToken));
}
```