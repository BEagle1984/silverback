---
title: DDD and Domain Events
permalink: /docs/quickstart/domain-events
toc: false
---

One of the core features of Silverback is the ability to publish the domain events as part of the `DbContext` save changes transaction in order to guarantee consistency.

The _Silverback.Core.Model_ package contains a sample implementation of a `DomainEntity` but you can also implement you own type.

```c#
using Silverback.Domain;

namespace Sample
{
    public class Basket : Entity, IAggregateRoot
    {
        private readonly List<BasketItem> _items = new List<BasketItem>();

        private Basket()
        {
        }

        [Key]
        public int Id { get; private set; }
        public IEnumerable<BasketItem> Items => _items.AsReadOnly();
        public Guid UserId { get; private set; }
        public DateTime Created { get; private set; }
        public DateTime? CheckoutDate { get; private set; }

        public static Basket Create(Guid userId)
        {
            return new Basket
            {
                UserId = userId,
                Created = DateTime.UtcNow
            };
        }

    ...

    public void Checkout()
    {
        CheckoutDate = DateTime.UtcNow;

        AddEvent<BasketCheckoutEvent>();
    }
}
```

The `AddEvent<TEvent>()` method adds the domain event to the `IDomainEntity` events collection, to be published by the when the entity is saved.

To enable this mechanism we just need to override the various `SaveChanges` methods to plug-in the `DbContextEventsPublisher` contained in the _Silverback.Core.EntityFrameworkCore_ package.

```c#
public class MyDbContext : DbContext
{
    private DbContextEventsPublisher<DomainEntity> _eventsPublisher;

    public ShopDbContext(IPublisher publisher)
    {
        InitEventsPublisher(publisher);
    }

    public ShopDbContext(DbContextOptions options, IPublisher publisher)
        : base(options)
    {
        InitEventsPublisher(publisher);
    }

    private void InitEventsPublisher(IPublisher publisher)
    {
        _eventsPublisher = new DbContextEventsPublisher<DomainEntity>(
            DomainEntityEventsAccessor.EventsSelector,
            DomainEntityEventsAccessor.ClearEventsAction,
            publisher,
            this);
    }

    public override int SaveChanges()
        => SaveChanges(true);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
        => _eventsPublisher.ExecuteSaveTransaction(() => base.SaveChanges(acceptAllChangesOnSuccess));

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(true, cancellationToken);

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        => _eventsPublisher.ExecuteSaveTransactionAsync(() =>
            base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken));
}
```