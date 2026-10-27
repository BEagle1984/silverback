---
uid: quickstart
title: Quickstart
---

# Quickstart

This guide gets you from an empty folder to an application producing and consuming messages through Apache Kafka in a few minutes.

## Prerequisites

- The [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) to run a local Kafka broker

Start a single-node Kafka broker listening on `localhost:9092`:

```bash
docker run -d --name kafka -p 9092:9092 apache/kafka:4.1.0
```

## Create the Project

Create a new worker service and add the Silverback Kafka integration package:

```bash
dotnet new worker -n SilverbackQuickstart
cd SilverbackQuickstart
dotnet add package Silverback.Integration.Kafka
```

## Define the Message

Messages are plain classes: no base class or interface is required.

```csharp
public class OrderCreatedEvent
{
    public int OrderId { get; set; }
}
```

## Configure Silverback

Replace the content of `Program.cs` with the following configuration: a producer publishing `OrderCreatedEvent` to the `orders` topic, a consumer reading from the same topic, and a subscriber handling the consumed messages.

```csharp
using Silverback.Configuration;
using Silverback.Messaging.Configuration;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer(producer => producer
            .Produce<OrderCreatedEvent>(endpoint => endpoint
                .ProduceTo("orders")))
        .AddConsumer(consumer => consumer
            .WithGroupId("quickstart")
            .AutoResetOffsetToEarliest()
            .Consume<OrderCreatedEvent>(endpoint => endpoint
                .ConsumeFrom("orders"))))
    .AddSingletonSubscriber<OrderCreatedEventHandler>();

builder.Services.AddHostedService<Worker>();

builder.Build().Run();
```

## Produce the Messages

Inject the <xref:Silverback.Messaging.Publishing.IPublisher> and publish the messages. Silverback routes them to the `orders` topic according to the producer configuration. Replace the generated `Worker.cs` with:

```csharp
using Silverback.Messaging.Publishing;

public class Worker : BackgroundService
{
    private readonly IPublisher _publisher;

    public Worker(IPublisher publisher)
    {
        _publisher = publisher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int orderId = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            await _publisher.PublishAsync(new OrderCreatedEvent { OrderId = ++orderId }, stoppingToken);

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
```

## Consume the Messages

The consumed messages are published to the internal message bus and forwarded to the matching subscribers. A subscriber is just a class with a public method accepting the message.

```csharp
public class OrderCreatedEventHandler
{
    private readonly ILogger<OrderCreatedEventHandler> _logger;

    public OrderCreatedEventHandler(ILogger<OrderCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public void Handle(OrderCreatedEvent message) =>
        _logger.LogInformation("Order {OrderId} created", message.OrderId);
}
```

## Run It

```bash
dotnet run
```

The log shows the orders being consumed, one per second. Stop the application with `Ctrl+C`: Silverback disconnects the clients gracefully.

> [!Tip]
> Producer and consumer usually live in different services. Splitting this example in two applications only requires moving the `AddProducer` and `AddConsumer` blocks to the respective service.

## Next Steps

- <xref:setup> – the available packages and the registration options.
- <xref:bus> – the in-memory message bus, subscribers and behaviors.
- <xref:producing> and <xref:consuming> – the producer and consumer configuration in depth.
- <xref:error-policies> – what happens when the processing fails.
- <xref:outbox> – produce reliably as part of your database transaction.
- <xref:testing> – test your application with the in-memory brokers.
- <xref:samples> – more complete examples.
