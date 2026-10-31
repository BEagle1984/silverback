---
uid: about
title: About Silverback
---

# About Silverback

Silverback is an open-source message bus and broker integration library for .NET, developed since 2018 to simplify building event-driven applications on top of Apache Kafka and MQTT.

- **Source code:** [github.com/BEagle1984/silverback](https://github.com/BEagle1984/silverback)
- **Packages:** [nuget.org](https://www.nuget.org/packages?q=Silverback)
- **Release notes:** <xref:releases-500>

## License

Silverback is licensed under the [MIT License](https://github.com/BEagle1984/silverback/blob/master/LICENSE): you are free to use it in both open-source and commercial projects.

## Contact

Silverback is maintained by **Sergio Aquilini**.

- <i class="bi bi-github"></i> [BEagle1984 on GitHub](https://github.com/BEagle1984)
- <i class="bi bi-linkedin"></i> [LinkedIn](https://www.linkedin.com/in/sergio-aquilini-689764140/)
- <i class="bi bi-envelope-at"></i> [contact@silverback-messaging.net](mailto:contact@silverback-messaging.net)

For questions and ideas, please use the [GitHub Discussions](https://github.com/BEagle1984/silverback/discussions); to report a bug, open an [issue](https://github.com/BEagle1984/silverback/issues). See also <xref:contributing>.

## Acknowledgements

Silverback is built on top of several outstanding open-source libraries.

### Message Brokers

- [Confluent's .NET Client for Apache Kafka](https://github.com/confluentinc/confluent-kafka-dotnet) – The Kafka client used by `Silverback.Integration.Kafka`, as well as the Schema Registry serializers (Avro, JSON and Protobuf) used by `Silverback.Integration.Kafka.SchemaRegistry`.
- [MQTTnet](https://github.com/dotnet/MQTTnet) – The MQTT client used by `Silverback.Integration.MQTT`.

### Storage

- [Entity Framework Core](https://github.com/dotnet/efcore) – Used by `Silverback.Storage.EntityFramework`.
- [Npgsql](https://github.com/npgsql/npgsql) – The PostgreSQL data provider used by `Silverback.Storage.PostgreSql`.
- [DistributedLock](https://github.com/madelson/DistributedLock) – Provides the PostgreSQL advisory locks used by `Silverback.Storage.PostgreSql`.
- [Microsoft.Data.Sqlite](https://github.com/dotnet/efcore/tree/main/src/Microsoft.Data.Sqlite.Core) – The SQLite data provider used by `Silverback.Storage.Sqlite`.

### Optional Integrations

- [Rx.NET](https://github.com/dotnet/reactive) – The reactive extensions used by `Silverback.Core.Rx` to expose the message bus as an observable stream.
- [Json.NET](https://github.com/JamesNK/Newtonsoft.Json) – The JSON serializer used by `Silverback.Integration.Newtonsoft`.

### Documentation

- [docfx](https://github.com/dotnet/docfx) – Generates this documentation website.
