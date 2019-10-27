# Silverback
[![Build Status](https://dev.azure.com/beagle1984/Silverback/_apis/build/status/BEagle1984.silverback?branchName=develop)](https://dev.azure.com/beagle1984/Silverback/_build/latest?definitionId=2&branchName=develop)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/BEagle1984/silverback/blob/master/LICENSE)

Silverback is a simple framework to build reactive, event-driven, microservices.

It includes an in-memory message bus that can be easily connected to a message broker to integrate with other microservices. At the moment only [Apache Kafka](https://kafka.apache.org/) is supported but other message brokers could be added without much effort.

Its main features are:
* Simple yet powerful message bus
* Abstracted and configurative integration with a message broker
* Apache Kafka integration
* DDD, Domain Events and Transactional Messaging
* Built-in error handling policies for consumers

## Installation

Silverback is split into multiple nuget packages available on nuget.org.

Package | Version
:--- | ---
Silverback.Core  | [![NuGet](http://img.shields.io/nuget/vpre/Silverback.Core.svg)](https://www.nuget.org/packages/Silverback.Core/)
Silverback.Core.Model  | [![NuGet](http://img.shields.io/nuget/vpre/Silverback.Core.Model.svg)](https://www.nuget.org/packages/Silverback.Core.Model/)
Silverback.Core.EntityFrameworkCore | [![NuGet](http://img.shields.io/nuget/vpre/Silverback.Core.EntityFrameworkCore.svg)](https://www.nuget.org/packages/Silverback.Core.EntityFrameworkCore/)
Silverback.Core.Rx | [![NuGet](http://img.shields.io/nuget/vpre/Silverback.Core.Rx.svg)](https://www.nuget.org/packages/Silverback.Core.Rx/)
Silverback.Integration | [![NuGet](http://img.shields.io/nuget/vpre/Silverback.Integration.svg)](https://www.nuget.org/packages/Silverback.Integration/)
Silverback.Integration.Kafka | [![NuGet](http://img.shields.io/nuget/vpre/Silverback.Integration.Kafka.svg)](https://www.nuget.org/packages/Silverback.Integration.Kafka/)
Silverback.Integration.Configuration | [![NuGet](http://img.shields.io/nuget/vpre/Silverback.Integration.Configuration.svg)](https://www.nuget.org/packages/Silverback.Integration.Configuration/)
Silverback.Integration.HealthChecks | [![NuGet](http://img.shields.io/nuget/vpre/Silverback.Integration.HealthChecks.svg)](https://www.nuget.org/packages/Silverback.Integration.HealthChecks/)


## Usage

Have a look at the [project's website](https://beagle1984.github.io/silverback/docs/architecture) for usage details, snippets and examples.

## License

This code is licensed under MIT license (see [LICENSE](https://github.com/BEagle1984/silverback/blob/master/LICENSE) file for details)
