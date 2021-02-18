---
uid: streaming
---

# Streaming

The <xref:Silverback.Messaging.Messages.IMessageStreamEnumerable`1> can be used to consume an endpoint in a streaming fashion and it is the only way to consume sequences (see for example [batch consuming](xref:inbound#batch-processing)). This stream will be forwarded to the subscribed method as soon as the first message is consumed and it is then asynchronously pushed with the next messages.
 
<xref:Silverback.Messaging.Messages.IMessageStreamEnumerable`1> implements both [IEnumerable<T>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1) and [IAsyncEnumerable<T>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1) and the subscriber method can either declare an <xref:Silverback.Messaging.Messages.IMessageStreamEnumerable`1>, an [IEnumerable<T>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1) or an [IAsyncEnumerable<T>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1) as argument.

Since the asynchronous and I/O bound nature of this stream it is recommended to take advantage of the [IAsyncEnumerable<T>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1) capabilities to asynchronously loop through the messages. 


```csharp
public class StreamSubscriber
{
    public async Task OnOrderStreamReceived(
        IAsyncEnumerable<OrderEvent> eventsStream)
    {
        await foreach(var orderEvent in eventsStream)
        {
            // ...process the event...
        }
    }
}
```

A single instance of <xref:Silverback.Messaging.Messages.IMessageStreamEnumerable`1> is created and published per each queue/topic/partition and the messages are acknowledged (committed) after a single iteration completes, unless sequencing (e.g. [batch consuming](xref:inbound#batch-processing)) is configured or a sequence is automatically recognized by Silverback (e.g. a dataset). In that case an instance is published per each sequence and the entire sequence is atomically committed.

# Rx (Observable)

The [Silverback.Core.Rx](https://www.nuget.org/packages/Silverback.Core.Rx) package adds the <xref:Silverback.Messaging.Messages.IMessageStreamObservable`1> that works like the <xref:Silverback.Messaging.Messages.IMessageStreamEnumerable`1> but implements [IObservable<T>](https://docs.microsoft.com/en-us/dotnet/api/system.iobservable-1) enabling the usage of [Rx.NET](https://github.com/dotnet/reactive).

# [Startup](#tab/rx-startup)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSilverback().AsObservable();
    }
}
```
# [Subscriber](#tab/rx-subscriber)
```csharp
public class StreamSubscriber
{
    public async Task OnOrderStreamReceived(
        IObservable<OrderEvent> eventsStream)
    {
        stream.Subscribe(...);
    }
}
```
***
