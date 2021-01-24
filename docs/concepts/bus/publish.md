---
uid: publish
---

# Publishing

## Basic Publishing

To publish the message you just need an instance of <xref:Silverback.Messaging.Publishing.IPublisher> (or derived interfaces if using [Silverback.Core.Model](https://www.nuget.org/packages/Silverback.Core.Model), as shown later on).

```csharp
using Silverback.Messaging.Publishing;

public class PublishingService
{
    private readonly IPublisher _publisher;

    public PublishingService(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task PublishSomething()
    {
        await _publisher.PublishAsync(new SampleMessage 
        { 
            Content = "whatever"
        });
    }
}
```

The publisher always exposes a synchronous and an asynchronous version of each method. The second option is of course to be preferred to take advantage of non-blocking async/await.

## Return values

In some cases you will of course return a response after having processed the message.

```csharp
public async Task<Report> PublishSomething()
{
    var result = await _publisher.PublishAsync(new ReportQuery() { ... });

    return result.Single();
}
```

> [!Important]
> Please note the required call to `Single()`, because Silverback allows you to have multiple subscribers for the same message and therefore collect multiple return values. This is not needed if using <xref:Silverback.Messaging.Publishing.IQueryPublisher> or <xref:Silverback.Messaging.Publishing.ICommandPublisher> described in the <xref:model> page.

## Batches

It is possible to publish multiple messages with a single call to [Publish](xref:Silverback.Messaging.Publishing.IPublisher#Silverback_Messaging_Publishing_IPublisher_Publish_IEnumerable_System_Object__System_Boolean_) or [PublishAsync](xref:Silverback.Messaging.Publishing.IPublisher#Silverback_Messaging_Publishing_IPublisher_PublishAsync_IEnumerable_System_Object__). The effect is very different than looping and calling the publish for each message since the collection will be handled as a batch, enabling parallel processing. It is usually suggested to publish multiple messages with the overloads accepting an [IEnumerable](https://docs.microsoft.com/en-us/dotnet/api/system.collections.ienumerable) and let the subscribers decide between parallel or sequential processing.

# [Publisher](#tab/batch-publisher)
```csharp
public async Task PublishBatch()
{
    await _publisher.PublishAsync(new [] 
    {
        new SomeEvent() { ... },
        new SomeEvent() { ... },
        new SomeEvent() { ... }
    });
}
```
# [Subscriber](#tab/batch-subscriber)
```csharp
public async Task OnSomeEvent(IEnumerable<SomeEvent> events)
{
    // Process events in batch
}
```
***


## Silverback.Core.Model

[Silverback.Core.Model](https://www.nuget.org/packages/Silverback.Core.Model) has been introduced in the previous page <xref:model>.

Each message type (<xref:Silverback.Messaging.Messages.IEvent>, <xref:Silverback.Messaging.Messages.ICommand>/<xref:Silverback.Messaging.Messages.ICommand`1> and <xref:Silverback.Messaging.Messages.IQuery`1>) also comes with its specialized <xref:Silverback.Messaging.Publishing.IPublisher> as quickly shown in the following sub-chapters.

### Events

The messages implementing <xref:Silverback.Messaging.Messages.IEvent>, <xref:Silverback.Domain.IDomainEvent> or <xref:Silverback.Messaging.Messages.IIntegrationEvent> can be published using an <xref:Silverback.Messaging.Publishing.IEventPublisher>.

```csharp
using Silverback.Messaging.Publishing;

public class PublishingService
{
    private readonly IEventPublisher _publisher;

    public PublishingService(IEventPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task PublishEvent()
    {
        var myEvent = new MyEvent() { ... };
        await _publisher.PublishAsync(myEvent);
    }
}
```

### Commands

The messages that implement <xref:Silverback.Messaging.Messages.ICommand>, <xref:Silverback.Messaging.Messages.ICommand`1> or <xref:Silverback.Messaging.Messages.IIntegrationCommand> can be published using an <xref:Silverback.Messaging.Publishing.ICommandPublisher>.

# [Without result](#tab/commands-publishingservice1)
```csharp
using Silverback.Messaging.Publishing;

public class PublishingService
{
    private readonly ICommandPublisher _publisher;

    public PublishingService(ICommandPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task ExecuteCommand()
    {
        var command = new MyCommand() { ... };
        await _publisher.ExecuteAsync(command);
    }
}
```
# [With result](#tab/commands-publishingservice2)
```csharp
using Silverback.Messaging.Publishing;

public class PublishingService
{
    private readonly ICommandPublisher _publisher;

    public PublishingService(ICommandPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task<MyResult> ExecuteCommand()
    {
        var command = new MyCommand() { ... };
        var result = await _publisher.ExecuteAsync(command);
        return result;
    }
}
```
***

### Queries

The <xref:Silverback.Messaging.Publishing.IQueryPublisher> ca be used to publish the messages implementing the <xref:Silverback.Messaging.Messages.IQuery`1> interface.

```csharp
using Silverback.Messaging.Publishing;

public class PublishingService
{
    private readonly IQueryPublisher _publisher;

    public PublishingService(IQueryPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task<MyResult> GetResults()
    {
        var query = new MyQuery() { ... };
        var result = await _publisher.ExecuteAsync(myQuery);
        return result;
    }
}
```
