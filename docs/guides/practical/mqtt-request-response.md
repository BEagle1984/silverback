---
uid: mqtt-request-response
---

# MQTT 5 Request-Response

MQTT 5 standardizes the request-response pattern with two message properties:

- **Response topic**: the topic the requester expects the response to be published to.
- **Correlation data**: an opaque binary value, copied by the responder into the response, that the requester uses to match the response with the original request.

Silverback maps both properties to internal headers (see <xref:Silverback.Messaging.Messages.MqttMessageHeaders>) and provides extension methods to set and read them. This guide shows how to wire the requester and the responder.

> [!Note]
> Response topic and correlation data are MQTT 5 features. MQTT 5 is the default protocol version; don't downgrade the client via `UseProtocolVersion` if you rely on them.

## Requester: Send the Request

The requester produces the request, specifying its response topic and the correlation data, and subscribes to the response topic.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddMqtt())
    .AddMqttClients(clients => clients
        .ConnectViaTcp("localhost")
        .AddClient("requester", client => client
            .WithClientId("requester-1")
            .Produce<GetTemperatureRequest>("temperature-requests", endpoint => endpoint
                .ProduceTo("devices/temperature/requests")
                .SetResponseTopic("devices/temperature/responses/requester-1")
                .SetCorrelationData(message => message?.RequestId.ToString()))
            .Consume<TemperatureResponse>("temperature-responses", endpoint => endpoint
                .ConsumeFrom("devices/temperature/responses/requester-1")
                .WithAtLeastOnceQoS())))
    .AddSingletonSubscriber<TemperatureResponseSubscriber>();
```

`SetResponseTopic` and `SetCorrelationData` offer several overloads: you can pass a fixed value or a function receiving the message or the <xref:Silverback.Messaging.Messages.IOutboundEnvelope`1>. The correlation data can be specified either as `string` (UTF-8 encoded) or as `byte[]`.

Alternatively, the values can be set per message when publishing:

```csharp
await publisher.WrapAndPublishAsync(
    request,
    envelope => envelope
        .SetMqttResponseTopic("devices/temperature/responses/requester-1")
        .SetMqttCorrelationData(request.RequestId.ToString()));
```

> [!Tip]
> Use a response topic that is unique per requester instance (e.g., including the client ID), so that each instance receives only the responses to its own requests.

## Responder: Send the Response

The responder consumes the request, reads the response topic and correlation data from the inbound envelope, and produces the response to the topic specified by the requester. Since the destination topic is only known at runtime, the response endpoint uses `ProduceToDynamicTopic`.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddMqtt())
    .AddMqttClients(clients => clients
        .ConnectViaTcp("localhost")
        .AddClient("responder", client => client
            .WithClientId("responder-1")
            .Consume<GetTemperatureRequest>("temperature-requests", endpoint => endpoint
                .ConsumeFrom("devices/temperature/requests")
                .WithAtLeastOnceQoS())
            .Produce<TemperatureResponse>("temperature-responses", endpoint => endpoint
                .ProduceToDynamicTopic())))
    .AddScopedSubscriber<TemperatureRequestHandler>();
```

```csharp
public class TemperatureRequestHandler
{
    private readonly IPublisher _publisher;

    private readonly ITemperatureSensor _sensor;

    public TemperatureRequestHandler(IPublisher publisher, ITemperatureSensor sensor)
    {
        _publisher = publisher;
        _sensor = sensor;
    }

    public async Task HandleAsync(IInboundEnvelope<GetTemperatureRequest> envelope)
    {
        string? responseTopic = envelope.GetMqttResponseTopic();

        if (responseTopic == null)
            return; // Nobody is waiting for a response

        byte[]? correlationData = envelope.GetMqttCorrelationData();

        TemperatureResponse response = new()
        {
            Temperature = await _sensor.ReadAsync()
        };

        await _publisher.WrapAndPublishAsync(
            response,
            outboundEnvelope => outboundEnvelope
                .SetMqttDestinationTopic(responseTopic)
                .SetMqttCorrelationData(correlationData));
    }
}
```

## Requester: Correlate the Response

Silverback doesn't provide a blocking request-response API: the response is consumed like any other message. To await the response in the code that sent the request, keep the pending requests in a singleton and complete them when the response arrives.

```csharp
public class PendingRequests
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<TemperatureResponse>> _requests = new();

    public Task<TemperatureResponse> Register(string correlationId) =>
        _requests.GetOrAdd(correlationId, _ => new TaskCompletionSource<TemperatureResponse>(TaskCreationOptions.RunContinuationsAsynchronously)).Task;

    public void Complete(string correlationId, TemperatureResponse response)
    {
        if (_requests.TryRemove(correlationId, out TaskCompletionSource<TemperatureResponse>? request))
            request.TrySetResult(response);
    }

    public void Remove(string correlationId) => _requests.TryRemove(correlationId, out _);
}

public class TemperatureResponseSubscriber
{
    private readonly PendingRequests _pendingRequests;

    public TemperatureResponseSubscriber(PendingRequests pendingRequests)
    {
        _pendingRequests = pendingRequests;
    }

    public void Handle(IInboundEnvelope<TemperatureResponse> envelope)
    {
        string? correlationId = envelope.GetMqttCorrelationDataAsString();

        if (correlationId != null && envelope.Message != null)
            _pendingRequests.Complete(correlationId, envelope.Message);
    }
}
```

Register `PendingRequests` as a singleton (`services.AddSingleton<PendingRequests>()`). Before publishing a request, call `Register(request.RequestId.ToString())` and then await the returned task, ideally combined with a timeout (e.g., `WaitAsync(TimeSpan.FromSeconds(10))`). Call `Remove` if no response is received in time.

## Caveats

- The response topic and correlation data are transmitted as MQTT 5 properties, not as user properties. Use `GetMqttResponseTopic`, `GetMqttCorrelationData` and `GetMqttCorrelationDataAsString` to read them on the consumer side.
- With QoS 1 (at least once) both the request and the response can be delivered more than once: make the responder idempotent and ignore responses whose correlation data is unknown.
- Responses for requests that timed out, or that were sent by a requester instance that was restarted, are consumed but cannot be matched to any pending request.

## Additional Resources

- [API Reference](xref:Silverback)
- <xref:producing>
- <xref:consuming>
- <xref:producing-headers>
- <xref:consuming-headers>
