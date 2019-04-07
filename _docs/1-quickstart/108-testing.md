---
title: Testing
permalink: /docs/quickstart/testing
toc: false
---

The _Silverback.Integration.InMemory_ package allows to perform end-to-end tests without having to integrate with a real message broker.

Here an example of an xUnit test build leveraging the `InMemoryBroker`.

```c#
public class InMemoryBrokerTests
{
    private readonly IServiceProvider _serviceProvider;

    // Configure DI during setup
    public InMemoryBrokerTests()
    {
        var services = new ServiceCollection();

        // Loggers are a prerequisite
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        // Enable the bus as usual
        services.AddBus();

        // Register the InMemoryBroker instead of any real broker (e.g. Kafka)
        services.AddBroker<InMemoryBroker>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void SampleTest()
    {
        var receivedMessages = new List<object>();

        _serviceProvider.GetRequiredService<BusConfigurator>()
            // Bind the subscribers under test (if needed)
            .Subscribe((IInboundMessage<TestMessage> msg) => receivedMessages.Add(msg))
            // Configure inbound and outbound endpoints
            .Connect(endpoints => endpoints
                .AddInbound(new KafkaConsumerEndpoint("test-topic"), settings: new InboundConnectorSettings
                {
                    UnwrapMessages = false
                })
                .AddOutbound<TestMessage>(new KafkaProducerEndpoint("test-topic")));

        // Publish some messages
        var publisher = _serviceProvider.GetRequiredService<IPublisher>();
        publisher.Publish(new TestMessage { Content = "hello!" });
        publisher.Publish(new TestMessage { Content = "hello 2!" });

        // Assert
        receivedMessages.Count.Should().Be(2);
    }
}
```