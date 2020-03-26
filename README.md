# Silverback
[![Build Status](https://dev.azure.com/beagle1984/Silverback/_apis/build/status/BEagle1984.silverback?branchName=develop)](https://dev.azure.com/beagle1984/Silverback/_build/latest?definitionId=2&branchName=develop) 
[![Azure DevOps tests (branch)](https://img.shields.io/azure-devops/tests/beagle1984/Silverback/2/develop)](https://dev.azure.com/beagle1984/Silverback/_build/latest?definitionId=2&branchName=develop)
[![GitHub issues by-label](https://img.shields.io/github/issues/beagle1984/silverback/bug?label=bugs)](https://github.com/BEagle1984/silverback/issues?q=is%3Aopen+is%3Aissue+label%3Abug)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/BEagle1984/silverback/blob/master/LICENSE)

A simple but feature-rich framework to build reactive/event-driven applications or microservices.

It includes an in-memory message bus that can be easily connected to a message broker to integrate with other applications or microservices. At the moment only [Apache Kafka](https://kafka.apache.org/) and [RabbitMQ](https://www.rabbitmq.com/) are supported but other message brokers could be added without much effort.

Its main features are:
* Simple yet powerful message bus
* Abstracted and configurative integration with a message broker
* Apache Kafka and RabbitMQ integration
* DDD, Domain Events and Transactional Messaging
* Outbox table pattern implementation
* Built-in error handling policies for consumers

## Installation

Silverback is split into multiple nuget packages available on nuget.org.

Package | Latest Version | Downloads
:--- | :---: | :---:
[Silverback.Core][Nuget-Core] | [![NuGet](http://img.shields.io/nuget/vpre/Silverback.Core.svg?label=)][Nuget-Core] | [![NuGet](https://img.shields.io/nuget/dt/Silverback.Core.svg?label=)][Nuget-Core]
[Silverback.Core.Model][Nuget-Core.Model] | [![NuGet](http://img.shields.io/nuget/vpre/Silverback.Core.Model.svg?label=)][Nuget-Core.Model] | [![NuGet](https://img.shields.io/nuget/dt/Silverback.Core.Model.svg?label=)][Nuget-Core.Model]
[Silverback.Core.EntityFrameworkCore][Nuget-Core.EntityFrameworkCore] | [![NuGet](http://img.shields.io/nuget/vpre/Silverback.Core.EntityFrameworkCore.svg?label=)][Nuget-Core.EntityFrameworkCore] | [![NuGet](https://img.shields.io/nuget/dt/Silverback.Core.EntityFrameworkCore.svg?label=)][Nuget-Core.EntityFrameworkCore]
[Silverback.Core.Rx][Nuget-Core.Rx] | [![NuGet](http://img.shields.io/nuget/vpre/Silverback.Core.Rx.svg?label=)][Nuget-Core.Rx] | [![NuGet](https://img.shields.io/nuget/dt/Silverback.Core.Rx.svg?label=)][Nuget-Core.Rx]
[Silverback.Integration][Nuget-Integration] | [![NuGet](http://img.shields.io/nuget/vpre/Silverback.Integration.svg?label=)][Nuget-Integration] | [![NuGet](https://img.shields.io/nuget/dt/Silverback.Integration.svg?label=)][Nuget-Integration]
[Silverback.Integration.Kafka][Nuget-Integration.Kafka] | [![NuGet](http://img.shields.io/nuget/vpre/Silverback.Integration.Kafka.svg?label=)][Nuget-Integration.Kafka] | [![NuGet](https://img.shields.io/nuget/dt/Silverback.Integration.Kafka.svg?label=)][Nuget-Integration.Kafka]
[Silverback.Integration.Kafka.SchemaRegistry][Nuget-Integration.Kafka.SchemaRegistry] | [![NuGet](http://img.shields.io/nuget/vpre/Silverback.Integration.Kafka.SchemaRegistry.svg?label=)][Nuget-Integration.Kafka.SchemaRegistry] | [![NuGet](https://img.shields.io/nuget/dt/Silverback.Integration.Kafka.SchemaRegistry.svg?label=)][Nuget-Integration.Kafka.SchemaRegistry]
[Silverback.Integration.RabbitMQ][Nuget-Integration.RabbitMQ] | [![NuGet](http://img.shields.io/nuget/vpre/Silverback.Integration.RabbitMQ.svg?label=)][Nuget-Integration.RabbitMQ] | [![NuGet](https://img.shields.io/nuget/dt/Silverback.Integration.RabbitMQ.svg?label=)][Nuget-Integration.RabbitMQ]
[Silverback.Integration.Configuration][Nuget-Integration.Configuration] | [![NuGet](http://img.shields.io/nuget/vpre/Silverback.Integration.Configuration.svg?label=)][Nuget-Integration.Configuration] | [![NuGet](https://img.shields.io/nuget/dt/Silverback.Integration.Configuration.svg?label=)][Nuget-Integration.Configuration]
[Silverback.Integration.HealthChecks][Nuget-Integration.HealthChecks] | [![NuGet](http://img.shields.io/nuget/vpre/Silverback.Integration.HealthChecks.svg?label=)](https://www.nuget.org/packages/Silverback.Integration.HealthChecks/) | [![NuGet](https://img.shields.io/nuget/dt/Silverback.Integration.HealthChecks.svg?label=)][Nuget-Integration.HealthChecks]

## Usage

Have a look at the [project's website](https://beagle1984.github.io/silverback/docs/architecture) for usage details, snippets and examples.

## Contributing

You are encouraged to contribute to Silverback! Please check out the [how to contribute](CONTRIBUTING.md) guide for guidelines about how to proceed.

## License

This code is licensed under MIT license (see [LICENSE](https://github.com/BEagle1984/silverback/blob/master/LICENSE) file for details)

[Nuget-Core]: https://www.nuget.org/packages/Silverback.Core/
[Nuget-Core.Model]: https://www.nuget.org/packages/Silverback.Core.Model/
[Nuget-Core.EntityFrameworkCore]: https://www.nuget.org/packages/Silverback.Core.EntityFrameworkCore/
[Nuget-Core.Rx]: https://www.nuget.org/packages/Silverback.Core.Rx/
[Nuget-Integration]: https://www.nuget.org/packages/Silverback.Integration/
[Nuget-Integration.Kafka]: https://www.nuget.org/packages/Silverback.Integration.Kafka/
[Nuget-Integration.Kafka.SchemaRegistry]: https://www.nuget.org/packages/Silverback.Integration.Kafka.SchemaRegistry/
[Nuget-Integration.RabbitMQ]: https://www.nuget.org/packages/Silverback.Integration.RabbitMQ/
[Nuget-Integration.Configuration]: https://www.nuget.org/packages/Silverback.Integration.Configuration/
[Nuget-Integration.HealthChecks]: https://www.nuget.org/packages/Silverback.Integration.HealthChecks/