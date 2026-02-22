# Silverback

Silverback is a message bus and broker integration library for .NET.
It helps you build event-driven architectures and asynchronous workflows with first-class support for **Apache Kafka** and **MQTT**.

Silverback aims to be both **high-level** (consistent configuration and developer experience) and **broker-aware**.
Kafka is a first-class citizen: features like partition-based parallelism, keys/partitioning, tombstones, Schema Registry integration, idempotency, and transactions are surfaced where they matter, instead of being abstracted away.

## Why Silverback

- **Kafka-first, not Kafka-only** – a consistent API across brokers, while still leveraging Kafka-specific capabilities.
- **Reliable by design** – transactional outbox, error policies, and storage-backed features.
- **Operational usability** – structured logging, diagnostics, and tracing.
- **Built-in cross-cutting features** – headers, validation, encryption, chunking, batching.
- **Testability** – in-memory broker mocks and end-to-end helpers.

Documentation, guides, and samples are available here: **https://silverback-messaging.net**

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

## Getting Started

Silverback is modular – reference only what you need.

### Packages

Core:

- **[Silverback.Core](https://www.nuget.org/packages/Silverback.Core/)** – message bus and core messaging components.
- **[Silverback.Core.Model](https://www.nuget.org/packages/Silverback.Core.Model/)** – message semantics for event-driven/CQRS scenarios.

Broker integration:

- **[Silverback.Integration.Kafka](https://www.nuget.org/packages/Silverback.Integration.Kafka/)** – Kafka support.
- **[Silverback.Integration.Mqtt](https://www.nuget.org/packages/Silverback.Integration.Mqtt/)** – MQTT support.

Optional features:

- **[Silverback.Core.Rx](https://www.nuget.org/packages/Silverback.Core.Rx/)** – Rx.NET integration.
- **[Silverback.Newtonsoft](https://www.nuget.org/packages/Silverback.Newtonsoft/)** – Newtonsoft.Json serialization.
- **[Silverback.Kafka.SchemaRegistry](https://www.nuget.org/packages/Silverback.Kafka.SchemaRegistry/)** – Confluent Schema Registry integration.

Storage (for outbox, client-side offsets, distributed locks):

- **[Silverback.Storage.PostgreSql](https://www.nuget.org/packages/Silverback.Storage.PostgreSql/)**
- **[Silverback.Storage.Sqlite](https://www.nuget.org/packages/Silverback.Storage.Sqlite/)**
- **[Silverback.Storage.EntityFramework](https://www.nuget.org/packages/Silverback.Storage.EntityFramework/)**
- **[Silverback.Storage.Memory](https://www.nuget.org/packages/Silverback.Storage.Memory/)**

Testing:

- **[Silverback.Integration.Kafka.Testing](https://www.nuget.org/packages/Silverback.Integration.Kafka.Testing/)**
- **[Silverback.Integration.Mqtt.Testing](https://www.nuget.org/packages/Silverback.Integration.Mqtt.Testing/)**

### Supported .NET Versions

Starting with v5, Silverback targets the latest .NET LTS version only.

### Quick Example (Kafka)

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer(producer => producer
            .Produce<MyMessage>(endpoint => endpoint.ProduceTo("my-topic")))
        .AddConsumer(consumer => consumer
            .Consume<MyMessage>(endpoint => endpoint.ConsumeFrom("my-topic"))));
```

## Usage

See the docs site for guides, API reference, and runnable examples:

- https://silverback-messaging.net

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

MIT License. See [LICENSE](https://github.com/BEagle1984/silverback/blob/master/LICENSE).
