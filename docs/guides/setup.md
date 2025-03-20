---
uid: setup
---

# Setting Up Silverback

Silverback is designed as a modular framework, allowing you to include only the components you need. This guide walks you through selecting the right packages and configuring the basics to get started.

## Adding the Required Packages

### Core Packages

- **[Silverback.Core](https://www.nuget.org/packages/Silverback.Core/)** – The essential package, including the message bus and fundamental messaging components.
- **[Silverback.Core.Model](https://www.nuget.org/packages/Silverback.Core.Model/)** – Enhances CQRS and event-driven architectures with improved semantics.

### Message Broker Integration

If you need to integrate with a message broker, choose the appropriate package:

- **[Silverback.Integration.Kafka](https://www.nuget.org/packages/Silverback.Integration.Kafka/)** – Adds Kafka support (implicitly includes Silverback.Core).
- **[Silverback.Integration.Mqtt](https://www.nuget.org/packages/Silverback.Integration.Mqtt/)** – Adds MQTT support (implicitly includes Silverback.Core).

### Additional Features

- **[Silverback.Core.Rx](https://www.nuget.org/packages/Silverback.Core.Rx/)** – Enables Rx.NET integration, allowing observables from the message bus stream.
- **[Silverback.Newtonsoft](https://www.nuget.org/packages/Silverback.Newtonsoft/)** – Supports serialization using Newtonsoft.Json instead of System.Text.Json.
- **[Silverback.Kafka.SchemaRegistry](https://www.nuget.org/packages/Silverback.Kafka.SchemaRegistry/)** – Provides integration with Schema Registry for Protobuf, Avro, or JSON serialization with schema support.

### Storage Options

Certain Silverback features rely on a storage mechanism. Choose the appropriate package based on your needs:

- **[Silverback.Storage.PostgreSql](https://www.nuget.org/packages/Silverback.Storage.PostgreSql/)** – Optimized for PostgreSQL, leveraging advisory locks.
- **[Silverback.Storage.Sqlite](https://www.nuget.org/packages/Silverback.Storage.Sqlite/)** – A lightweight option, ideal for testing.
- **[Silverback.Storage.EntityFramework](https://www.nuget.org/packages/Silverback.Storage.EntityFramework/)** – Works with Entity Framework, supporting all EF-compatible relational databases.
- **[Silverback.Storage.Memory](https://www.nuget.org/packages/Silverback.Storage.Memory/)** – An in-memory storage option, useful for testing.

### Testing Support

For unit testing message-driven applications, you can use in-memory broker mocks:

- **[Silverback.Integration.Kafka.Testing](https://www.nuget.org/packages/Silverback.Integration.Kafka.Testing/)** – Provides an in-memory Kafka mock, simulating partitioning, offsets management, and other broker behaviors.
- **[Silverback.Integration.Mqtt.Testing](https://www.nuget.org/packages/Silverback.Integration.Mqtt.Testing/)** – Provides an in-memory MQTT mock to test MQTT-related logic.

## Registering and Configuring Silverback

Once you’ve installed the necessary packages, configure Silverback within your application's Dependency Injection (DI) container.

You can use either `AddSilverback` or `ConfigureSilverback` to register and configure Silverback.

- **`AddSilverback`** should typically be called once as the initial setup.
- **`ConfigureSilverback`** allows extending the configuration in different parts of the bootstrap process, such as when configuring feature slices separately.

This is a basic example of configuring Silverback. Many settings and customization options are available depending on the features you want to use. For example, a message broker is optional, and you don’t need to configure both Kafka and MQTT unless required for your use case.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options
        .AddKafka()
        .AddMqtt())
    .AddSingletonSubscriber<MyMessageHandler>();
```

- **`AddSilverback`** enables the message bus and allows chaining additional configurations via the fluent API.
- **`WithConnectionToMessageBroker`** configures the connection to the message brokers (both Kafka and MQTT in this case).
- **`AddSingletonSubscriber`** registers a message handler to process incoming messages.

## Next Steps

With these steps, Silverback is now set up and ready to use! You can explore more advanced topics such as message processing, broker integration, and custom configurations in the following guides.

