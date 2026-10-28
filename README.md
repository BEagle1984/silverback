# Silverback

**Simplicity at its core. Flexibility at its peak. Effortless messaging for .NET.**

Silverback is a message bus and broker integration library to build event-driven architectures with first-class support for **Apache Kafka** and **MQTT**.

[![NuGet version](https://img.shields.io/nuget/v/Silverback.Core?label=NuGet&color=2f6fb3)](https://www.nuget.org/packages/Silverback.Core)
[![NuGet downloads](https://img.shields.io/nuget/dt/Silverback.Core?label=downloads&color=2f6fb3)](https://www.nuget.org/packages/Silverback.Core)
[![License: MIT](https://img.shields.io/github/license/BEagle1984/silverback?color=2f6fb3)](https://github.com/BEagle1984/silverback/blob/master/LICENSE)

Documentation, guides, and samples are available at **https://silverback-messaging.net**.

## Produce and Consume in a Few Lines

Configure the broker connection, the producer and consumer endpoints, and a subscriber. Silverback takes care of serialization, headers, error handling and delivery.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer(producer => producer
            .Produce<OrderCreated>(endpoint => endpoint
                .ProduceTo("orders")))
        .AddConsumer(consumer => consumer
            .WithGroupId("billing")
            .Consume<OrderCreated>(endpoint => endpoint
                .ConsumeFrom("orders")
                .OnError(policy => policy.Retry(3).ThenSkip()))))
    .AddSingletonSubscriber<OrderCreatedHandler>();
```

```csharp
// Publish: the message is routed to the "orders" topic
await publisher.PublishAsync(new OrderCreated(orderId));

// Consume: the subscriber is invoked for each consumed message
public class OrderCreatedHandler
{
    public Task HandleAsync(OrderCreated message) => ...;
}
```

Follow the [Quickstart](https://silverback-messaging.net/guides/quickstart.html) for a complete, runnable walkthrough.

## Why Silverback

- **Kafka-first, not Kafka-only** – a consistent, fluent API for Kafka and MQTT that still surfaces partitions, keys, tombstones and transactions.
- **Reliable by design** – transactional outbox, error policies (retry, skip, move) and storage-backed offsets and locks.
- **Built for throughput** – partition-based parallelism, batch processing, streaming and chunking of large payloads.
- **Cross-cutting features** – serialization, Schema Registry (JSON, Avro, Protobuf), headers, validation and end-to-end encryption.
- **Observable** – structured logging, distributed tracing via `System.Diagnostics` and health checks.
- **Testable** – in-memory Kafka and MQTT brokers and testing helpers for fast and reliable end-to-end tests.

## Packages

Silverback is modular – reference only what you need.

Core:

- **[Silverback.Core](https://www.nuget.org/packages/Silverback.Core/)** – message bus and core messaging components.
- **[Silverback.Core.Model](https://www.nuget.org/packages/Silverback.Core.Model/)** – message semantics for event-driven/CQRS scenarios.

Broker integration:

- **[Silverback.Integration.Kafka](https://www.nuget.org/packages/Silverback.Integration.Kafka/)** – Kafka support.
- **[Silverback.Integration.MQTT](https://www.nuget.org/packages/Silverback.Integration.MQTT/)** – MQTT support.

Optional features:

- **[Silverback.Core.Rx](https://www.nuget.org/packages/Silverback.Core.Rx/)** – Rx.NET integration.
- **[Silverback.Integration.Newtonsoft](https://www.nuget.org/packages/Silverback.Integration.Newtonsoft/)** – Newtonsoft.Json serialization.
- **[Silverback.Integration.Kafka.SchemaRegistry](https://www.nuget.org/packages/Silverback.Integration.Kafka.SchemaRegistry/)** – Confluent Schema Registry
  integration (Avro, Protobuf and JSON).
- **[Silverback.Integration.HealthChecks](https://www.nuget.org/packages/Silverback.Integration.HealthChecks/)** – health checks for the consumers and the outbox.

Storage (for outbox, client-side offsets, distributed locks):

- **[Silverback.Storage.PostgreSql](https://www.nuget.org/packages/Silverback.Storage.PostgreSql/)**
- **[Silverback.Storage.Sqlite](https://www.nuget.org/packages/Silverback.Storage.Sqlite/)**
- **[Silverback.Storage.EntityFramework](https://www.nuget.org/packages/Silverback.Storage.EntityFramework/)**
- **[Silverback.Storage.Memory](https://www.nuget.org/packages/Silverback.Storage.Memory/)**

Testing:

- **[Silverback.Integration.Testing](https://www.nuget.org/packages/Silverback.Integration.Testing/)** – broker-independent testing helpers.
- **[Silverback.Integration.Kafka.Testing](https://www.nuget.org/packages/Silverback.Integration.Kafka.Testing/)** – in-memory Kafka broker.
- **[Silverback.Integration.MQTT.Testing](https://www.nuget.org/packages/Silverback.Integration.MQTT.Testing/)** – in-memory MQTT broker.

## Supported .NET Versions

Starting with v5, Silverback targets the **latest .NET LTS** (Long-Term Support) version only. It can run on newer compatible .NET versions (including STS releases), but older frameworks are no longer targeted or supported.

## Documentation

- [Quickstart](https://silverback-messaging.net/guides/quickstart.html) – produce and consume via Kafka in five minutes.
- [Guides](https://silverback-messaging.net/guides/setup.html) – setup, message bus, producing, consuming and more.
- [Samples](https://silverback-messaging.net/samples/intro.html) – runnable Kafka and MQTT examples.
- [Migrating from v4](https://silverback-messaging.net/guides/migration-v5.html) – step-by-step upgrade guide to Silverback 5.
- [FAQ and Troubleshooting](https://silverback-messaging.net/guides/faq.html) – answers to the most common questions.
- [API Reference](https://silverback-messaging.net/api/Silverback.html) – the complete public API.

The v4 documentation is still available at [silverback-messaging.net/v4](https://silverback-messaging.net/v4/).

## Project Status

### Continuous Build

[![Continuous Build Status](https://dev.azure.com/beagle1984/Silverback/_apis/build/status/continuous?branchName=master)](https://dev.azure.com/beagle1984/Silverback/_build/latest?definitionId=5&branchName=master)
[![Tests Status (release/5.0.0)](https://img.shields.io/azure-devops/tests/beagle1984/Silverback/5/master)](https://dev.azure.com/beagle1984/Silverback/_build/latest?definitionId=5&branchName=master)

### Sonar Build

[![Sonar Build Status](https://dev.azure.com/beagle1984/Silverback/_apis/build/status/sonar?branchName=master)](https://dev.azure.com/beagle1984/Silverback/_build/latest?definitionId=6&branchName=master)

#### Quality Metrics

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=silverback&metric=alert_status)](https://sonarcloud.io/dashboard?id=silverback)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=silverback&metric=sqale_rating)](https://sonarcloud.io/dashboard?id=silverback)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=silverback&metric=reliability_rating)](https://sonarcloud.io/dashboard?id=silverback)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=silverback&metric=security_rating)](https://sonarcloud.io/dashboard?id=silverback)

[![Lines of Code](https://sonarcloud.io/api/project_badges/measure?project=silverback&metric=ncloc)](https://sonarcloud.io/dashboard?id=silverback)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=silverback&metric=coverage)](https://sonarcloud.io/dashboard?id=silverback)
[![Duplicated Lines (%)](https://sonarcloud.io/api/project_badges/measure?project=silverback&metric=duplicated_lines_density)](https://sonarcloud.io/dashboard?id=silverback)

[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=silverback&metric=bugs)](https://sonarcloud.io/dashboard?id=silverback)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=silverback&metric=code_smells)](https://sonarcloud.io/dashboard?id=silverback)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=silverback&metric=vulnerabilities)](https://sonarcloud.io/dashboard?id=silverback)
[![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=silverback&metric=sqale_index)](https://sonarcloud.io/dashboard?id=silverback)

### Activity

[![GitHub bugs](https://img.shields.io/github/issues/beagle1984/silverback/bug?label=bugs)](https://github.com/BEagle1984/silverback/issues?q=is%3Aopen+is%3Aissue+label%3Abug)
[![GitHub issues](https://img.shields.io/github/issues/beagle1984/silverback)](https://github.com/BEagle1984/silverback/issues?q=is%3Aopen+is)
[![GitHub pull requests](https://img.shields.io/github/issues-pr/beagle1984/silverback)](https://github.com/BEagle1984/silverback/pulls)
[![GitHub last commit](https://img.shields.io/github/last-commit/beagle1984/silverback)](https://github.com/BEagle1984/silverback/commits)

## Repository Solutions and Tools

### Solutions

| Solution | Contents |
| --- | --- |
| [Silverback.sln](Silverback.sln) | Library source projects, unit and E2E tests, and code/documentation generators |
| [Silverback.Samples.sln](samples/Silverback.Samples.sln) | Runnable usage examples; see the [samples guide](samples/README.md) |
| [Silverback.Tests.Extended.sln](tests/extended/Silverback.Tests.Extended.sln) | Interactive testbench, real-broker integration and stress tests, general benchmarks and version comparisons, with references to the library sources |

### Testing and Development Tools

| Tool | Purpose |
| --- | --- |
| [Interactive testbench](tests/extended/README.md#interactive-testbench) | Windows WPF application for producing messages and managing Docker consumers |
| [Integration tests](tests/extended/README.md#integration-tests) | xUnit tests against real Kafka and MQTT brokers, including stress workloads and diagnostic controls |
| [Benchmarks](tests/extended/README.md#benchmarks) | General performance benchmarks and current-versus-historical Silverback comparisons |
| [UpdateOlderVersions.ps1](tests/extended/UpdateOlderVersions.ps1) | Scaffold another historical benchmark project |
| [coverage.ps1](coverage.ps1) | Build and test the main solution with coverage, then generate and open an HTML report |
| [docker-compose.yaml](docker-compose.yaml) | Local Kafka, Schema Registry, MQTT and PostgreSQL infrastructure, with administration UIs |
| [nuget/Update.ps1](nuget/Update.ps1) | Build local Silverback packages for package-based development and samples |
| [.NET tool manifest](.config/dotnet-tools.json) | Repository-local ReportGenerator installation used by the coverage script |

### Code and Documentation Generators

The four generator applications are included in the main solution and write generated content to standard output. Shared generator support lives in [Silverback.Tools.Generators.Common](tools/Silverback.Tools.Generators.Common).

| Tool | Purpose |
| --- | --- |
| [KafkaConfigProxies](tools/Silverback.Tools.Generators.KafkaConfigProxies) | Generate Kafka configuration wrappers and builders; also supports Schema Registry configuration |
| [MqttConfigProxies](tools/Silverback.Tools.Generators.MqttConfigProxies) | Generate MQTT configuration wrappers and builders |
| [Docs.Headers](tools/Silverback.Tools.Generators.Docs.Headers) | Generate message-header reference tables |
| [Docs.LogEvents](tools/Silverback.Tools.Generators.Docs.LogEvents) | Generate log-event reference tables |
| [docs/build.ps1](docs/build.ps1) | Build and serve the DocFX documentation locally; run from the docs directory |
| [docs/publish.ps1](docs/publish.ps1) | Publish the generated site through a separate gh-pages checkout; supports -NoPush |

Build automation is defined in [the main pipeline](azure-pipelines.yml), [the Sonar pipeline](azure-pipelines.sonar.yml), and [the samples/extended-tests pipeline](azure-pipelines.other-projects.yml).

## Community

- [Issues](https://github.com/BEagle1984/silverback/issues) – report bugs or suggest improvements.
- [Discussions](https://github.com/BEagle1984/silverback/discussions) – ask questions and share ideas.
- [CONTRIBUTING.md](CONTRIBUTING.md) – how to contribute.

## License

Silverback is open source, licensed under the MIT License and maintained by Sergio Aquilini. See [LICENSE](https://github.com/BEagle1984/silverback/blob/master/LICENSE).
