# <img src="https://github.com/BEagle1984/silverback/raw/master/docs/images/logo.png" alt="Silverback"> 

A simple but feature-rich framework to build reactive/event-driven applications or microservices.

It includes an in-memory message bus that can be easily connected to a message broker to integrate with other applications or microservices. At the moment only [Apache Kafka](https://kafka.apache.org/) and [RabbitMQ](https://www.rabbitmq.com/) are supported but other message brokers could be added without much effort.

Its main features are:
* Simple yet powerful message bus
* Abstracted integration with a message broker
* Apache Kafka and RabbitMQ integration
* DDD, domain events and transactional messaging
* Built-in error handling policies for consumers

Discover more in the [project's website][docs-site].

## Project Status

### Build

[![Build Status](https://dev.azure.com/beagle1984/Silverback/_apis/build/status/continuous?branchName=master)](https://dev.azure.com/beagle1984/Silverback/_build/latest?definitionId=5&branchName=master) 
[![Azure DevOps tests (branch)](https://img.shields.io/azure-devops/tests/beagle1984/Silverback/5/master)](https://dev.azure.com/beagle1984/Silverback/_build/latest?definitionId=5&branchName=master)

### Quality

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

## Installation

Silverback is split into multiple nuget packages available on nuget.org.

| Package | Stats |
:--- | :---
[Silverback.Core][Nuget-Core] | [![NuGet](https://buildstats.info/nuget/Silverback.Core?includePreReleases=true)](https://www.nuget.org/packages/Silverback.Core)
[Silverback.Core.Model][Nuget-Core.Model] | [![NuGet](https://buildstats.info/nuget/Silverback.Core.Model?includePreReleases=true)](https://www.nuget.org/packages/Silverback.Core.Model)
[Silverback.Core.EntityFrameworkCore][Nuget-Core.EntityFrameworkCore] | [![NuGet](https://buildstats.info/nuget/Silverback.Core.EntityFrameworkCore?includePreReleases=true)](https://www.nuget.org/packages/Silverback.Core.EntityFrameworkCore)
[Silverback.Core.Rx][Nuget-Core.Rx] | [![NuGet](https://buildstats.info/nuget/Silverback.Core.Rx?includePreReleases=true)](https://www.nuget.org/packages/Silverback.Core.Rx)
[Silverback.Integration][Nuget-Integration] | [![NuGet](https://buildstats.info/nuget/Silverback.Integration?includePreReleases=true)](https://www.nuget.org/packages/Silverback.Integration)
[Silverback.Integration.Testing][Nuget-Integration.Testing] | [![NuGet](https://buildstats.info/nuget/Silverback.Integration.Testing?includePreReleases=true)](https://www.nuget.org/packages/Silverback.Integration.Testing)
[Silverback.Integration.Kafka][Nuget-Integration.Kafka] | [![NuGet](https://buildstats.info/nuget/Silverback.Integration.Kafka?includePreReleases=true)](https://www.nuget.org/packages/Silverback.Integration.Kafka)
[Silverback.Integration.Kafka.SchemaRegistry][Nuget-Integration.Kafka.SchemaRegistry] | [![NuGet](https://buildstats.info/nuget/Silverback.Integration.Kafka.SchemaRegistry?includePreReleases=true)](https://www.nuget.org/packages/Silverback.Integration.Kafka.SchemaRegistry)
[Silverback.Integration.Kafka.Testing][Nuget-Integration.Kafka.Testing] | [![NuGet](https://buildstats.info/nuget/Silverback.Integration.Kafka.Testing?includePreReleases=true)](https://www.nuget.org/packages/Silverback.Integration.Kafka.Testing)
[Silverback.Integration.MQTT][Nuget-Integration.MQTT] | [![NuGet](https://buildstats.info/nuget/Silverback.Integration.MQTT?includePreReleases=true)](https://www.nuget.org/packages/Silverback.Integration.MQTT)
[Silverback.Integration.MQTT.Testing][Nuget-Integration.MQTT.Testing] | [![NuGet](https://buildstats.info/nuget/Silverback.Integration.MQTT.Testing?includePreReleases=true)](https://www.nuget.org/packages/Silverback.Integration.MQTT.Testing)
[Silverback.Integration.RabbitMQ][Nuget-Integration.RabbitMQ] | [![NuGet](https://buildstats.info/nuget/Silverback.Integration.RabbitMQ?includePreReleases=true)](https://www.nuget.org/packages/Silverback.Integration.RabbitMQ)
[Silverback.Integration.HealthChecks][Nuget-Integration.HealthChecks] | [![NuGet](https://buildstats.info/nuget/Silverback.Integration.HealthChecks?includePreReleases=true)](https://www.nuget.org/packages/Silverback.Integration.HealthChecks)
[Silverback.Integration.Newtonsoft][Nuget-Integration.Newtonsoft] | [![NuGet](https://buildstats.info/nuget/Silverback.Integration.Newtonsoft?includePreReleases=true)](https://www.nuget.org/packages/Silverback.Integration.Newtonsoft)

## Usage

Have a look at the [project's website][docs-site] for usage details, API documentation and samples.

## Contributing

You are encouraged to contribute to Silverback! Please check out the [how to contribute](CONTRIBUTING.md) guide for guidelines about how to proceed.

## License

This code is licensed under MIT license (see [LICENSE](https://github.com/BEagle1984/silverback/blob/master/LICENSE) file for details)

[docs-site]: https://silverback-messaging.net
[Nuget-Core]: https://www.nuget.org/packages/Silverback.Core/
[Nuget-Core.Model]: https://www.nuget.org/packages/Silverback.Core.Model/
[Nuget-Core.EntityFrameworkCore]: https://www.nuget.org/packages/Silverback.Core.EntityFrameworkCore/
[Nuget-Core.Rx]: https://www.nuget.org/packages/Silverback.Core.Rx/
[Nuget-Integration]: https://www.nuget.org/packages/Silverback.Integration/
[Nuget-Integration.Testing]: https://www.nuget.org/packages/Silverback.Integration.Testing/
[Nuget-Integration.Kafka]: https://www.nuget.org/packages/Silverback.Integration.Kafka/
[Nuget-Integration.Kafka.SchemaRegistry]: https://www.nuget.org/packages/Silverback.Integration.Kafka.SchemaRegistry/
[Nuget-Integration.Kafka.Testing]: https://www.nuget.org/packages/Silverback.Integration.Kafka.Testing/
[Nuget-Integration.MQTT]: https://www.nuget.org/packages/Silverback.Integration.MQTT/
[Nuget-Integration.MQTT.Testing]: https://www.nuget.org/packages/Silverback.Integration.MQTT.Testing/
[Nuget-Integration.RabbitMQ]: https://www.nuget.org/packages/Silverback.Integration.RabbitMQ/
[Nuget-Integration.HealthChecks]: https://www.nuget.org/packages/Silverback.Integration.HealthChecks/
[Nuget-Integration.Newtonsoft]: https://www.nuget.org/packages/Silverback.Integration.Newtonsoft/
