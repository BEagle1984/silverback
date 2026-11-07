---
uid: kafka-admin-client
---

# Kafka Admin Client

Use the Confluent.Kafka `IAdminClient` for administrative operations, such as creating topics or reading the cluster metadata. Silverback doesn't wrap this client: it only creates it through the <xref:Silverback.Messaging.Broker.Kafka.IConfluentAdminClientFactory>, reusing the connection settings of your producers and consumers and routing its errors and logs to the Silverback logger.

## Get a Client for a Producer or Consumer

Inject the <xref:Silverback.Messaging.Broker.Kafka.IConfluentAdminClientFactory> and call the method matching the way your clients are identified. The admin client uses the bootstrap servers, credentials and other client settings of the matching producer or consumer.

Method | Argument
:-- | :--
`GetClientForProducer` | The name specified in `AddProducer`
`GetClientForConsumer` | The name specified in `AddConsumer`
`GetClientForProducerEndpoint` | The endpoint friendly name or the topic name
`GetClientForConsumerEndpoint` | The endpoint friendly name or the name of one of the consumed topics

```csharp
public class TopicsInspector
{
    private readonly IConfluentAdminClientFactory _adminClientFactory;

    public TopicsInspector(IConfluentAdminClientFactory adminClientFactory)
    {
        _adminClientFactory = adminClientFactory;
    }

    public int GetPartitionsCount()
    {
        using IAdminClient adminClient = _adminClientFactory.GetClientForConsumerEndpoint("my-endpoint");

        return adminClient.GetMetadata("my-topic", TimeSpan.FromSeconds(10)).Topics[0].Partitions.Count;
    }
}
```

> [!Important]
> Each call creates a new client with its own connections and threads. The caller owns the client and must dispose it, so avoid creating one for each request or message.

If several producers or consumers use the same topic, a lookup by topic name returns any one of them, so use the friendly name if they connect to different clusters. A producer endpoint with a dynamic target topic can be found by friendly name only.

The `GetClient` overloads also create a client from an existing <xref:Silverback.Messaging.Broker.IProducer> or <xref:Silverback.Messaging.Broker.IConsumer>, from a producer or consumer configuration, or from dedicated settings.

```csharp
using IAdminClient adminClient = _adminClientFactory.GetClient(
    client => client.WithBootstrapServers("PLAINTEXT://localhost:9092"));
```

## Create Topics at Startup

The producers and consumers are available after the clients have been configured, so a lookup performed earlier, for example in a hosted service that starts before Silverback, fails with an `InvalidOperationException`.

Use an <xref:Silverback.Messaging.Broker.Callbacks.IBrokerClientsConfiguredCallback> to run your code after the clients have been configured and before they connect.

```csharp
public class TopicsCreator : IBrokerClientsConfiguredCallback
{
    private readonly IConfluentAdminClientFactory _adminClientFactory;

    public TopicsCreator(IConfluentAdminClientFactory adminClientFactory)
    {
        _adminClientFactory = adminClientFactory;
    }

    public async Task OnBrokerClientsConfiguredAsync()
    {
        using IAdminClient adminClient = _adminClientFactory.GetClientForProducerEndpoint("my-endpoint");

        try
        {
            await adminClient.CreateTopicsAsync(
            [
                new TopicSpecification
                {
                    Name = "my-topic",
                    NumPartitions = 6,
                    ReplicationFactor = 3
                }
            ]);
        }
        catch (CreateTopicsException ex) when (ex.Results.All(result =>
            result.Error.Code is ErrorCode.NoError or ErrorCode.TopicAlreadyExists))
        {
            // The topic already exists
        }
    }
}
```

Creating topics requires permissions that the application user often doesn't have in production, so consider registering such a callback only in the environments where it's needed.

Register the callback with the other broker client callbacks (see <xref:broker-callbacks>).

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer(producer => producer
            .Produce<MyMessage>("my-endpoint", endpoint => endpoint
                .ProduceTo("my-topic"))))
    .AddSingletonBrokerClientCallback<TopicsCreator>();
```

## Testing

When using the mocked Kafka broker, the factory returns a mocked admin client backed by the in-memory topics. It only supports `GetMetadata` and `CreateTopicsAsync` (without the `ValidateOnly` option), every other method throws a `NotSupportedException` (see <xref:testing>).

## Additional Resources

- [API Reference](xref:Silverback)
- <xref:broker>
- <xref:broker-callbacks>
- <xref:testing>
