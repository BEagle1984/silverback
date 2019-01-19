# Silverback

Silverback is a simple framework to build reactive, event-driven, microservices.

It includes an in-memory message bus that can be easily connected to a message broker to integrate with other microservices. At the moment only [Apache Kafka](https://kafka.apache.org/) is supported but other message brokers could be added without much effort.

Its main features are:
* Simple yet powerful message bus
* Abstracted and configurative integration with a message broker
* Apache Kafka integration
* DDD, Domain Events and Transactional Messaging
* Built-in error handling policies for consumers
* Configuration through fluent API or external configuration (`Microsoft.Extensions.Configuration`)

## Installation

Silverback is split into multiple nuget packages available on nuget.org.

Package | Version
:--- | ---
Silverback.Core  | [![NuGet](http://img.shields.io/nuget/v/Silverback.Core.svg)](https://www.nuget.org/packages/Silverback.Core/)
Silverback.Core.Model  | [![NuGet](http://img.shields.io/nuget/v/Silverback.Core.Model.svg)](https://www.nuget.org/packages/Silverback.Core.Model/)
Silverback.Core.EntityFrameworkCore | [![NuGet](http://img.shields.io/nuget/v/Silverback.Core.EntityFrameworkCore.svg)](https://www.nuget.org/packages/Silverback.Core.EntityFrameworkCore/)
Silverback.Core.Rx | [![NuGet](http://img.shields.io/nuget/v/Silverback.Core.Rx.svg)](https://www.nuget.org/packages/Silverback.Core.Rx/)
Silverback.Integration | [![NuGet](http://img.shields.io/nuget/v/Silverback.Integration.svg)](https://www.nuget.org/packages/Silverback.Integration/)
Silverback.Integration.EntityFrameworkCore | [![NuGet](http://img.shields.io/nuget/v/Silverback.Integration.EntityFrameworkCore.svg)](https://www.nuget.org/packages/Silverback.Integration.EntityFrameworkCore/)
Silverback.Integration.Kafka | [![NuGet](http://img.shields.io/nuget/v/Silverback.Integration.Kafka.svg)](https://www.nuget.org/packages/Silverback.Integration.Kafka/)
Silverback.Integration.Configuration | [![NuGet](http://img.shields.io/nuget/v/Silverback.Integration.Configuration.svg)](https://www.nuget.org/packages/Silverback.Integration.Configuration/)

## Usage

Have a look at the [project's website](https://beagle1984.github.io/silverback/docs/architecture) for usage details, snippets and examples.

## License

This code is licensed under MIT license (see [LICENSE](https://github.com/BEagle1984/silverback/blob/master/LICENSE) file for details)
