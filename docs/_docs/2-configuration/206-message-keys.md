---
title: Message Keys
permalink: /docs/configuration/message-keys

toc: false
---

Silverback must be able to get a unique key for the integration messages in order to be able to use the exactly-once inbound or the deferred outbound connectors.

The default implementation of `IMessageKeyProvider` looks for a public property called either `Id` or `MessageId` and supports `Guid` or `String` as data type.

If needed you can implement your own `IMessageKeyProvider` and register it for dependency injection.

```c#
public class SampleMessageKeyProvider : IMessageKeyProvider
{
    public bool CanHandle(object message) => message is SampleMessage;

    public string GetKey(object message) => ((SampleMessage)message).Sequence;

    public void EnsureKeyIsInitialized(object message)
    {
        var sampleMessage = (SampleMessage)message;

        if (sampleMessage.Sequence = 0)
            sempleMessage.Sequence = SequenceHelper.GetNext();
    }
}
```
```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<IMessageKeyProvider, SampleMessageKeyProvider>();
    
    services.AddBus();
```