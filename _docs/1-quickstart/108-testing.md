---
title: Testing
permalink: /docs/quickstart/testing
toc: true
---

The `Silverback.Integration.InMemory` package allows to perform end-to-end tests without having to integrate with a real message broker.

## Unit Tests

Here an example of an xUnit test build using the `InMemoryBroker`.

```csharp
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
            .WithInMemoryBroker()
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

The following code shows the most simple integration test possible, in which an object is published to the broker and e.g. a subscriber is called.

```csharp
public class IntegrationTests : IClassFixture<WebApplicationFactory<Startup>>
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
                services.OverrideWithInMemoryBroker();
            });
        };
    }

    [Fact]
    public async Task SampleTest()
    {
        // Arrange

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

As topics represents APIs there might be more complex scenarios in which one wants to test the serialization as well to ensure compatibility.
The code below shows a test which assumes that an use case is to consume from a topic, transform the record and produce to another topic.

```csharp
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
                services.OverrideWithInMemoryBroker();
            });
        };
    }

    [Fact]
    public async Task SampleTest()
    {

        // Arrange
        const string record = @"{
            ""FIRST_NAME"": ""Xy"",
            ""LAST_NAME"":""Zz"",
            ""AGE"":32
        }";

        byte[] recordBytes = Encoding.UTF8.GetBytes(record);
        using IServiceScope scope = _webApplicationFactory.Services.CreateScope();
        var broker = scope.ServiceProvider.GetRequiredService<IBroker>();

        // Internal events are directly emitted to the bus.
        // Events for which an endpoint is configured are emitted as IOutboundEnvelope<T> to the bus.
        IList<IOutboundEnvelope<Person>> externalEvents = new List<IOutboundEnvelope<Person>>();
        scope.ServiceProvider.GetRequiredService<BusConfigurator>()
            .Subscribe<IOutboundEnvelope<MappedPerson>>(e => externalEvents.Add(e));

        // Calling this producer is like there would be an "incoming" record.
        IProducer producer = broker.GetProducer(new KafkaProducerEndpoint("MyTopicFromWhichTheApplicationConsumes"));

        // Act
        await producer.ProduceAsync(recordBytes);

        // Assert
        externalEvents.Count.Should().Be(1);

        IOutboundEnvelope<MappedPerson> message = externalEvents.Single();
        message.Endpoint.Name.Should().Be("MyTopicToWhichTheApplicationWrites");
        string actualMessage = Encoding.UTF8.GetString(message.RawMessage);

        const string expectedMessage = @"{
            ""FirstName"": ""Xy"",
            ""LastName"":""Zz"",
            ""Age"":32
        }";

        // Might you want to create a own FluentAssertionExtension, to do something like this.
        actualMessage.Should().BeEquivalentJsonTo(expectedMessage);
    }
}

```
