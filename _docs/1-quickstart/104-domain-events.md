---
title: DDD and Domain Events
permalink: /docs/quickstart/domain-events
toc: false
---

To allow publishing the domain events through Silberback, the domain entities have to implement the `IDomainEntity` interface. Alternatively you can just extend the `Silverback.Domain.Entity` class.

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

To enable this mechanism we just need to override the various `SaveChanges` methods to plug-in the `DbContextEventsPublisher`.

```c#
public class MyDbContext : DbContext
{
    private readonly IEventPublisher<IEvent> _eventPublisher;

    public ShopDbContext(IEventPublisher<IEvent> eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }

    public ShopDbContext(DbContextOptions options, IEventPublisher<IEvent> eventPublisher) : base(options)
    {
        _eventPublisher = eventPublisher;
    }

    public override int SaveChanges() => SaveChanges(true);

    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        DbContextEventsPublisher.ExecuteSaveTransaction(this, () => base.SaveChanges(acceptAllChangesOnSuccess), _eventPublisher);

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        SaveChangesAsync(true, cancellationToken);

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) =>
        DbContextEventsPublisher.ExecuteSaveTransactionAsync(this, () => base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken), _eventPublisher);
}
```

For this use case you need of course **Silverback.Core.EntityFrameworkCore**.