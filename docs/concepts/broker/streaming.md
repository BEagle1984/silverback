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

# Notes, suggestions and insights

* The stream will be pushed with messages as they are read from the message broker. Since the I/O bound nature of the operation you should obviously prefer to subscribe to an [IAsyncEnumerable<T>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1) instead of an [IEnumerable<T>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1) and in any case loop asynchronously (`await foreach` or similar approach).
* If the sequence is interrupted because the application is disconnecting or an error occurred in another subscriber, the [IEnumerator](https://docs.microsoft.com/en-us/dotnet/api/system.collections.ienumerator) will throw an [OperationCancelledException](https://docs.microsoft.com/en-us/dotnet/api/system.operationcanceledexception). Handle it if you need to gracefully abort or cleanup.
* Throwing an exception while enumerating a sequence (e.g. a [BatchSequence](xref:Silverback.Messaging.Sequences.Batch.BatchSequence)) will cause it to be aborted and handled according to the defined [error policies](xref:inbound#error-handling). If you just break the iteration and the subscriber return, the operation will be considered successful instead and the sequence will be committed.

