# <img src="https://github.com/BEagle1984/silverback/raw/master/docs/images/logo.png" alt="Silverback">

Silverback is a **powerful, elegant, and feature-rich message bus for .NET**, designed to simplify asynchronous messaging, event-driven architectures, and microservice communication. With seamless integration for **Apache Kafka** and **MQTT**, it offers robust features for **reliability, consistency, and scalability**.

Whether you're building a **small microservice, a large-scale enterprise solution, or anything in between**, Silverback provides the tools to make messaging effortless and reliable.

## Why Choose Silverback?

Silverback is designed for **serious workloads**, offering enterprise-grade capabilities with a rich feature set optimized for **performance, resilience, and scalability**.

### Key Features

🔀 **Powerful Message Bus**\
A simple but powerful in-memory message bus enables seamless communication between components, featuring **Rx.NET** support for reactive programming.

🔗 **Seamless Message Broker Integration**\
Silverback makes it easy to integrate with Kafka and MQTT, providing a streamlined and developer-friendly API to build event-driven architectures with minimal setup and configuration.

🚀 **Kafka-Optimized Messaging**\
Unlike generic messaging libraries, Silverback is built specifically for Kafka, leveraging its unique capabilities for high-throughput, exactly-once semantics, and partitioned processing. While Silverback also supports MQTT, Kafka is a first-class citizen, and the framework is highly optimized to take full advantage of its power.

📤 **Transactional Outbox**\
Ensures message consistency by linking database transactions with messaging, preventing message loss and guaranteeing atomicity.

⚠️ **Advanced Error Handling**\
Define flexible strategies to **retry, skip, or move messages** based on custom policies, ensuring robustness in failure scenarios.

📦 **Batch Processing & Chunking**\
Enhances efficiency by processing messages in bulk or splitting large messages into smaller chunks, which are automatically reassembled on the receiving end.

⚡ **Domain-Driven Design (DDD) Support**\
Automates domain event publishing when entities are persisted, ensuring seamless integration with message brokers for event-driven workflows.

✅ **Exactly-Once Processing**\
Ensures each message is consumed and processed exactly once, preventing duplicate processing and maintaining data integrity.

🔍 **Distributed Tracing**\
Leverages **System.Diagnostics** for full visibility into message flow and distributed transaction tracking.

🧪 **Testability**\
Provides in-memory mocks for **Kafka** and **MQTT**, along with powerful helpers for efficient unit testing.

✨ **And much more!**\
Silverback is highly extensible, making it the go-to messaging framework for .NET developers who want to harness Kafka’s full potential while maintaining flexibility for other brokers.

Discover more in the [project's website][docs-site].

## Project Status

### Continuous Build

[![Continuous Build Status](https://dev.azure.com/beagle1984/Silverback/_apis/build/status/continuous?branchName=release%2F5.0.0)](https://dev.azure.com/beagle1984/Silverback/_build/latest?definitionId=5&branchName=release%2F5.0.0)
[![Tests Status (release/5.0.0)](https://img.shields.io/azure-devops/tests/beagle1984/Silverback/5/release%2F5.0.0)](https://dev.azure.com/beagle1984/Silverback/_build/latest?definitionId=5&branchName=release%2F5.0.0)

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

## Usage

Have a look at the [project's website][docs-site] for usage details, API documentation and samples.

## Contributing

You are encouraged to contribute to Silverback! Please check out the [how to contribute](CONTRIBUTING.md) guide for guidelines about how to
proceed.

## License

This code is licensed under MIT license (see [LICENSE](https://github.com/BEagle1984/silverback/blob/master/LICENSE) file for details)

[docs-site]: https://silverback-messaging.net
