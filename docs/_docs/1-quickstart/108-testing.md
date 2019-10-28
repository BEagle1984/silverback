---
title: Testing
permalink: /docs/quickstart/testing
toc: true
---

The `Silverback.Integration.InMemory` package allows to perform end-to-end tests without having to integrate with a real message broker.

## Unit Tests

Here an example of an xUnit test build using the `InMemoryBroker`.

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

        services
            // Register Silverback as usual
            .AddSilverback()
            // Register the InMemoryBroker instead of 
            // the real broker (e.g. KafkaBroker)
            .WithConnectionTo<InMemoryBroker>()
            // Register the subscriber under test
            .AddScopedSubscriber<MySubscriber>();

        // ...register all other types you need...
        
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void SampleTest()
    {
        // Arrange
        
        // Configure the Bus
        _serviceProvider.GetRequiredService<BusConfigurator>()
            // Configure inbound and outbound endpoints
            .Connect(endpoints => endpoints
                .AddInbound(new KafkaConsumerEndpoint("test-topic"));

        // Create a producer to push to test-topic
        var producer = _serviceProvider
            .GetRequiredService<IBroker>()
            .GetProducer(new KafkaProducerEndpoint("test-topic"));

        // Act
        producer.Produce(new TestMessage { Content = "hello!" });
        producer.Produce(new TestMessage { Content = "hello 2!" });

        // Assert
        // ...your assertions...
    }
}
```

## Integration Tests

An alternative technique is to leverage the [ASP.NET Core integration tests](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests) to perform a full test based on the real configuration applied in the application's startup class.

```c#
public class IntegrationTests 
    : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly WebApplicationFactory<Startup> _factory;

    public IntegrationTests(WebApplicationFactory<Startup> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Replace the usual broker (e.g. KafkaBroker) 
                // with the InMemoryBroker
                services.AddSingleton<IBroker, InMemoryBroker>();
            });
        };
    }

    [Fact]
    public async Task SampleTest()
    {
        // Arrange

        // This call is required to startup the environment
        _factory.CreateClient();

        // Resolve a producer to push to test-topic
        var producer = _factory.Server.Host.Services
            .GetRequiredService<IBroker>()
            .GetProducer(new KafkaProducerEndpoint("tst-topic"));

        // Act
        await producer.ProduceAsync(new TestMessage { Content = "abc" });

        // Assert
        // ...your assertions...
    }
}
```