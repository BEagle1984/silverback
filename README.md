# <img src="https://github.com/BEagle1984/silverback/raw/master/graphics/Exports/1x/Logo-Full.png" width="400" alt="Silverback"> 

A simple but feature-rich framework to build reactive/event-driven applications or microservices.

It includes an in-memory message bus that can be easily connected to a message broker to integrate with other applications or microservices. At the moment only [Apache Kafka](https://kafka.apache.org/) and [RabbitMQ](https://www.rabbitmq.com/) are supported but other message brokers could be added without much effort.

Its main features are:
* Simple yet powerful message bus
* Abstracted integration with a message broker
* Apache Kafka and RabbitMQ integration
* DDD, Domain Events and Transactional Messaging
* Outbox table pattern implementation
* Built-in error handling policies for consumers

Discover more in the [project's website][docs-site].

## Project Status

### Build

[![Build Status](https://dev.azure.com/beagle1984/Silverback/_apis/build/status/BEagle1984.silverback?branchName=master)](https://dev.azure.com/beagle1984/Silverback/_build/latest?definitionId=2&branchName=master) 
[![Azure DevOps tests (branch)](https://img.shields.io/azure-devops/tests/beagle1984/Silverback/2/master)](https://dev.azure.com/beagle1984/Silverback/_build/latest?definitionId=2&branchName=master)

### Quality

[![Sonar Cloud Quality Gate](https://sonarcloud.io/api/project_badges/measure?branch=master&project=silverback&metric=alert_status)](https://sonarcloud.io/dashboard?branch=master&id=silverback)
[![Sonar Cloud Quality Gate](https://sonarcloud.io/api/project_badges/measure?branch=master&project=silverback&metric=coverage)](https://sonarcloud.io/dashboard?branch=master&id=silverback)
[![Sonar Cloud Reliability Rate](https://sonarcloud.io/api/project_badges/measure?branch=master&project=silverback&metric=reliability_rating)](https://sonarcloud.io/dashboard?branch=master&id=silverback)
[![Sonar Cloud Security Rate](https://sonarcloud.io/api/project_badges/measure?branch=master&project=silverback&metric=security_rating)](https://sonarcloud.io/dashboard?branch=master&id=silverback)
[![Sonar Cloud Maintainability Rate](https://sonarcloud.io/api/project_badges/measure?branch=master&project=silverback&metric=sqale_rating)](https://sonarcloud.io/dashboard?branch=master&id=silverback)
[![Sonar Cloud Duplicated Code](https://sonarcloud.io/api/project_badges/measure?branch=master&project=silverback&metric=duplicated_lines_density)](https://sonarcloud.io/dashboard?branch=master&id=silverback)

### Activity

[![GitHub bugs](https://img.shields.io/github/issues/beagle1984/silverback/bug?label=bugs)](https://github.com/BEagle1984/silverback/issues?q=is%3Aopen+is%3Aissue+label%3Abug)
[![GitHub issues](https://img.shields.io/github/issues/beagle1984/silverback)](https://github.com/BEagle1984/silverback/issues?q=is%3Aopen+is)
[![GitHub pull requests](https://img.shields.io/github/issues-pr/beagle1984/silverback)](https://github.com/BEagle1984/silverback/pulls)
![GitHub last commit](https://img.shields.io/github/last-commit/beagle1984/silverback)

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
[Silverback.Integration.HealthChecks][Nuget-Integration.HealthChecks] | [![NuGet](http://img.shields.io/nuget/vpre/Silverback.Integration.HealthChecks.svg?label=)](https://www.nuget.org/packages/Silverback.Integration.HealthChecks/) | [![NuGet](https://img.shields.io/nuget/dt/Silverback.Integration.HealthChecks.svg?label=)][Nuget-Integration.HealthChecks]

## Usage

Have a look at the [project's website][docs-site] for usage details, snippets and examples.

## Contributing

You are encouraged to contribute to Silverback! Please check out the [how to contribute](CONTRIBUTING.md) guide for guidelines about how to proceed.

## License

This code is licensed under MIT license (see [LICENSE](https://github.com/BEagle1984/silverback/blob/master/LICENSE) file for details)

[docs-site]: https://beagle1984.github.io/silverback/
[Nuget-Core]: https://www.nuget.org/packages/Silverback.Core/
[Nuget-Core.Model]: https://www.nuget.org/packages/Silverback.Core.Model/
[Nuget-Core.EntityFrameworkCore]: https://www.nuget.org/packages/Silverback.Core.EntityFrameworkCore/
[Nuget-Core.Rx]: https://www.nuget.org/packages/Silverback.Core.Rx/
[Nuget-Integration]: https://www.nuget.org/packages/Silverback.Integration/
[Nuget-Integration.Kafka]: https://www.nuget.org/packages/Silverback.Integration.Kafka/
[Nuget-Integration.Kafka.SchemaRegistry]: https://www.nuget.org/packages/Silverback.Integration.Kafka.SchemaRegistry/
[Nuget-Integration.RabbitMQ]: https://www.nuget.org/packages/Silverback.Integration.RabbitMQ/
[Nuget-Integration.HealthChecks]: https://www.nuget.org/packages/Silverback.Integration.HealthChecks/
