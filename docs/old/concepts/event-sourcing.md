---
uid: event-sourcing
---

# Event Sourcing

`Silverback.EventSourcing` is a basic implementation of an event store that perfectly integrates within the Silverback ecosystem. At the moment only a version using Entity Framework Core is implemented, allowing to store the events in a database but other implementations may be added in the future.

## Configuration

The only needed configuration is the call to `UseDbContext<TDbContext>` when initializing Silverback.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSilverback().UseDbContext<MyDbContext>()
    }
}
```

## Creating the Event Store

Creating an event store is very straightforward and requires basically just 3 components: a domain entity model, the event store model and a repository.

### Domain Entity model

The domain entity have to extend `EventSourcingDomainEntity` (or a custom class implementing `IEventSourcingDomainEntity`).
The two generic type parameters refer to the type of the key (entity unique identifier) and the base type for the domain events (can be omited if you don't need domain events).

```csharp
public class Person : EventSourcingDomainEntity<int, PersonDomainEvent>
{
    public Person()
    {
    }

    public Person(IReadOnlyCollection<IEntityEvent> events) : base(events)
    {
    }

    public string Name { get; private set; }
    public string SocialSecurityNumber { get; private set; }
    public int Age { get; private set; }
    public string PhoneNumber { get; private set; }
}
```

> [!Important]
> The domain entity must have a constructor able to rebuild the entity state from the stored events.

The `AddAndApplyEvent` protected method must be used to add new events.

```csharp
public class Person : EventSourcingDomainEntity<int, PersonDomainEvent>
{
    public void ChangeName(string newName) =>
        AddAndApplyEvent(new NameChangedEvent
        {
            NewName = newName
        });

    public void ChangeAge(int newAge) =>
        AddAndApplyEvent(new AgeChangedEvent
        {
            NewAge = newAge
        });

    public void ChangePhoneNumber(string newPhoneNumber) =>
        AddAndApplyEvent(new PhoneNumberChangedEvent
        {
            NewPhoneNumber = newPhoneNumber
        });
}
```

An _Apply_ method is needed for each event type to modify the entity current state according to the described mutation.

```csharp
public class Person : EventSourcingDomainEntity<int, PersonDomainEvent>
{
    private void Apply(NameChangedEvent @event) => Name = @event.NewName;
 
    private void Apply(AgeChangedEvent @event) => Age = @event.NewAge;

    private void Apply(PhoneNumberChangedEvent @event, bool isReplaying)
    {
        PhoneNumber = @event.NewPhoneNumber;

        // Fire domain event only if the event is new
        if (!isReplaying)
            AddEvent<PhoneNumberChangedDomainEvent>();
    }
}
```

> [!Note]
> The apply method can be private but it must have a specific signature: its name must begin with _"Apply"_ and have a parameter of the specific event type (or base type).
It can also receive an additional boolean parameter (`isReplaying`) that will let you differentiate between new events and events that are being reapplied because loaded from the store.

The events are just models inheriting from `EntityEvent` (or another custom class implementing `IEntityEvent`).

```csharp
public class NameChangedEvent : EntityEvent 
{ 
    public string NewName { get; set; } 
}

public class AgeChangedEvent : EntityEvent 
{ 
    public int NewAge { get; set; } 
}

public class PhoneNumberChangedEvent : EntityEvent 
{ 
    public string NewPhoneNumber { get; set; } 
}
```

### Event Store model

The event store basically consists of an _EventStore_ entity and related event (they either inherit from `EventStoreEntity` and `EventEntity` or implement the interfaces `IEventStoreEntity` and `IEventEntity` respectively).

```csharp
public class PersonEventStore : EventStoreEntity<PersonEvent>
{
    [Key]
    public int Id { get; set; }

    public string SocialSecurityNumber { get; set; }
}

public class PersonEvent : EventEntity
{
    [Key]
    public int Id { get; private set; }
}
```

> [!Note]
> The event store record can be extended with extra fields (see `SocialSecurityNumber` in the example above) and those will be automatically set with the value of the matching propertyi in the domain entity (unless the mapping method is overridden in the repository implementing a custom logic).

> [!Important]
> It is advised to add some indexes and a concurrency token, to ensure proper performance and consistency.

A DbSet must also be mapped to the defined event store entity and that's it.

```csharp
public class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<PersonEventStore> Persons { get; set; }
}
```

### EventStore repository

The repository is the component that is storing the domain entity in form of single events, being able to rebuild it afterwards.

The repository must inherit from `DbContextEventStoreRepository` and the 4 generic type parameters refer respectively to:
* the domain entity
* its unique key
* the event store entity
* its related event entity

```csharp
public class PersonEventStoreRepository
    : DbContextEventStoreRepository<Person, int, PersonEventStore, PersonEvent>
{
    public PersonEventStoreRepository(DbContext dbContext)
        : base(dbContext)
    {
    }
}
```

## Storing and retrieving entities

Using the `EventStoreRepository` to store and retrieve domain entities is fairly simple. Have a look at the following code snippet to get an idea.

```csharp
public class PersonService
{
    private readonly MyDbContext _dbContext;
    private readonly PersonEventStoreRepository _repository =
        new PersonEventStoreRepository(_dbContext);

    public async Task<Person> CreatePerson(string name, int age)
    {
        var person = new Person();
        person.ChangeName("Sergio");
        person.ChangeAge(35);

        person = await _repository.StoreAsync(person);
        await _dbContext.SaveChangesAsync();

        return person;
    }

    public async Task<Person> ChangePhoneNumber(
        int personId,
        string newPhoneNumber)
    {
        var person = _repository.Get(p => p.Id == personId);

        person.ChangePhoneNumber(newPhoneNumber);

        person = await _repository.StoreAsync(person);
        await _dbContext.SaveChangesAsync();

        return person;
    }
}
```

## Merging events / handling conflicts

You may need to merge events coming from different sources and/or being received with a certain latency. In the example below the _Apply_ method checks whether another (newer) conflicting event was added already in the meantime.

```csharp
private void Apply(NameChangedEvent @event, bool isReplaying)
{
    // Skip if a newer event exists
    if (!isReplaying && Events.Any(e => 
        e is NameChangedEvent &&
        e.Timestamp > @event.Timestamp))
    {
        return;
    }

    Name = @event.NewName;
}
```
