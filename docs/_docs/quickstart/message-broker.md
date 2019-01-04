---
title: Connecting to a Message Broker
permalink: /docs/quickstart/message-broker
---

To connect Silverback to a message broker we need a reference to **Silverback.Integration**, plus the concrete implementation (**Silverback.Integration.Kafka** in this example). We can then add the broker to the DI and configure the connected endpoints. 

# Sample Configuration

The following example is very simple and there are of course many more configurations and possibilities. Some more details are given in the dedicated _Broker Configuration Explained_ section.

```c#
public void ConfigureServices(IServiceCollection services)
{
    ...

    services
        .AddBus()
        .AddBroker<KafkaBroker>(options => options
            .AddInboundConnector()
            .AddOutboundConnector());
    ...
}

public void Configure(..., IBrokerEndpointsConfigurationBuilder endpoints)
{
    endpoints
        .AddInbound(
            new KafkaEndpoint("basket-events")
            {
                Configuration = new Confluent.Kafka.ConsumerConfig
                {
                    BootstrapServers = "PLAINTEXT://kafka:9092",
                    GroupId = "order-service"
                }
            })
        .AddInbound(
            new KafkaEndpoint("payment-events")
            {
                Configuration = new Confluent.Kafka.ConsumerConfig
                {
                    BootstrapServers = "PLAINTEXT://kafka:9092",
                    GroupId = "order-service"
                }
            })
        .AddOutbound<IIntegrationEvent>(
            new KafkaEndpoint("order-events")
            {
                Configuration = new Confluent.Kafka.ProducerConfig
                {
                    BootstrapServers = "PLAINTEXT://kafka:9092"
                }
            })
        .Broker.Connect();
```

`AddInbound` is used to automatically relay the incoming messages to the internal bus and they can therefore be subscribed as seen in the previous chapters.

`AddOutbound` works the other way around and subscribes to the internal bus to forward the integration messages to the message broker.

`Broker.Connect()` actually creates and starts all the consumers.

# Graceful shutdown

It is important to properly close the consumers using the `Disconnect` method before exiting. The offsets have to be committed and the broker has to be notified (it will then proceed to reassign the partitions as needed).

```c#
public void Configure(..., IBrokerEndpointsConfigurationBuilder endpoints, IApplicationLifetime appLifetime)
{
    endpoints
        ...
        Broker.Connect();

    appLifetime.ApplicationStopping.Register(() => endpoints.Broker.Disconnect());
```