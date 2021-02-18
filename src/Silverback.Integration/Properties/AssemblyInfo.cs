// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Silverback.Integration.Kafka")]
[assembly: InternalsVisibleTo("Silverback.Integration.Mqtt")]
[assembly: InternalsVisibleTo("Silverback.Integration.RabbitMQ")]

[assembly: InternalsVisibleTo("Silverback.Integration.Tests")]
[assembly: InternalsVisibleTo("Silverback.Integration.Configuration.Tests")]
[assembly: InternalsVisibleTo("Silverback.Integration.Kafka.Tests")]
[assembly: InternalsVisibleTo("Silverback.Integration.MQTT.Tests")]
[assembly: InternalsVisibleTo("Silverback.Integration.RabbitMQ.Tests")]
[assembly: InternalsVisibleTo("Silverback.Integration.Newtonsoft")]
[assembly: InternalsVisibleTo("Silverback.Tests.Benchmarks")]
[assembly: InternalsVisibleTo("Silverback.Tests.Performance")]
[assembly: InternalsVisibleTo("Silverback.Tests.Common.Integration")]
