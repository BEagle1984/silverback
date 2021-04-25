---
uid: testing
---

# Testing

Silverback ships a mocked version of the message broker implementations on a different nuget package:
* [Silverback.Integration.Kafka.Testing](https://www.nuget.org/packages/Silverback.Integration.Kafka.Testing)
* _(coming soon)_ [Silverback.Integration.RabbitMQ.Testing](https://www.nuget.org/packages/Silverback.Integration.RabbitMQ.Testing)

These packages allow to perform end-to-end tests without having to integrate with a real message broker.

## Unit Tests

Here an example of an xUnit test built using [Silverback.Integration.Kafka.Testing](https://www.nuget.org/packages/Silverback.Integration.Kafka.Testing).

```csharp
public class KafkaTests
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
            // Register the mocked KafkaBroker
            .WithConnectionTo(config => config.AddMockedKafka())
            // Configure inbound and outbound endpoints
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                    {
                        config.BootstrapServers = "PLAINTEXT://tests"; 
                    })
                .AddOutbound<InventoryEvent>(endpoint => endpoint
                    .ProduceTo("test-topic"))
                .AddInbound(endpoint => endpoint
                    .ConsumeFrom("test-topic")
                    .Configure(config => 
                        {
                            config.GroupId = "my-test-consumer"
                        })))
            // Register the subscriber under test
            .AddScopedSubscriber<MySubscriber>();

        // ...register all other types you need...

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void SampleTest()
    {
        // Arrange

        // Connect the broker
        _serviceProvider.GetRequiredService<IBroker>().Connect();

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

Mocking the message broker is especially interesting for the integration tests, where you probably leverage the [ASP.NET Core integration tests](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests) to perform a full test based on the real configuration applied in the application's startup class.

The following code shows the simplest integration test possible, in which an object is published to the broker and e.g. a subscriber is called.

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
                // Replace the usual broker (KafkaBroker)
                // with the mocked version
                services.UseMockedKafka();
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

## Testing helper

The testing helpers (such has <xref:Silverback.Testing.IKafkaTestingHelper>) contain some methods that simplify testing with the message broker, given it's asynchronous nature.

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
                services.UseMockedKafka();
            });
        };
    }

    [Fact]
    public async Task SampleTest()
    {
        // Arrange

        // Resolve the IKafkaTestingHelper (used below)
        var testingHelper = _factory.Server.Host.Services
            .GetRequiredService<IKafkaTestingHelper>();

        // Resolve a producer to push to test-topic
        var producer = testingHelper.Broker
            .GetProducer(new KafkaProducerEndpoint("tst-topic"));

        // Act
        await producer.ProduceAsync(new TestMessage { Content = "abc" });

        // Wait until all messages have been consumed and 
        // committed before asserting
        await testingHelper.WaitUntilAllMessagesAreConsumedAsync();

        // Assert
        // ...your assertions...
    }
}
```

## IntegrationSpy

The <xref:Silverback.Testing.IIntegrationSpy> ships with the [Silverback.Integration.Testing](https://www.nuget.org/packages/Silverback.Integration.Testing) package (referenced by the other Integration.Testing.* packages) and can be used to inspect all outbound and inbound messages.

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
                services
                    .ConfigureSilverback()
                    .UseMockedKafka()
                    .AddIntegrationSpy();
            });
        };
    }

    [Fact]
    public async Task SampleTest()
    {
        // Arrange
        var testingHelper = _factory.Server.Host.Services
            .GetRequiredService<IKafkaTestingHelper>();

        var producer = testingHelper.Broker
            .GetProducer(new KafkaProducerEndpoint("tst-topic"));

        // Act
        await producer.ProduceAsync(new TestMessage { Content = "abc" });

        // Wait until all messages have been consumed and 
        // committed before asserting
        await testingHelper.WaitUntilAllMessagesAreConsumedAsync();

        // Assert
        testingHelper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        testingHelper.Spy.InboundEnvelopes.Should().HaveCount(1);
        testingHelper.Spy.InboundEnvelopes[0].Message.As<TestMessage>
            .Content.Should().Be("abc");
    }
}
```

## Mocked Kafka

Many aspects of the Kafka broker have been mocked to replicated as much as possible the behavior you have when connected with the real broker. This new implementation supports commits, kafka events, offset reset, partitioning, rebalance, etc.

The implementation revolves around the <xref:Silverback.Messaging.Broker.Kafka.Mocks.IInMemoryTopicCollection>. This type is registered as singleton and can be resolved in the tests to gain access to the <xref:Silverback.Messaging.Broker.Kafka.Mocks.IInMemoryTopic> instances directly and inspect their partitions, offsets, etc.
Alternatively the <xref:Silverback.Messaging.Broker.Kafka.Mocks.IInMemoryTopic> can be retrieved via the `GetTopic` method of the <xref:Silverback.Testing.IKafkaTestingHelper>.

### Partitioning

By default 5 partitions will be created per each topic being mocked. This number can be configured as shown in the following snippet. The setting is per broker and there's currently no way to configure each topic independently.

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
                services
                    .UseMockedKafka(options => options
                        .WithDefaultPartitionsCount(10));
            });
        };
    }
}
