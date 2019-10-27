---
title: Samples
permalink: /docs/source/samples
toc: false
---

A solution containing a few examples can be found [here on GitHub](https://github.com/BEagle1984/silverback/tree/develop/samples/Examples).

It includes a sample consumer (`Silverback.Examples.ConsumerA`) and a sample producer (`Silverback.Examples.Main`) implementing several common use cases. Just run both console applications to see the samples in action.

## Kafka

Of course you need a running Kafka instance for the samples to work. Run the following commands to quickly setup a basic Kafka cluster running on the local Docker.

```bash
docker network create confluent
```

```bash
docker run -d --net=confluent --name=zookeeper -e ZOOKEEPER_CLIENT_PORT=2181 confluentinc/cp-zookeeper:5.0.1
```

```bash
docker run -d --net=confluent --name=kafka -e KAFKA_ZOOKEEPER_CONNECT=zookeeper:2181 -e KAFKA_ADVERTISED_LISTENERS=PLAINTEXT://localhost:9092 -e KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR=1 -p 9092:9092 confluentinc/cp-kafka:5.0.1
```

## SQL Server

A SQL Server instance is also necessary to be able to run the samples. You can run it in docker as well.

```bash
docker run -d --name=mssql -e ACCEPT_EULA=Y -e SA_PASSWORD=mssql2017. -e MSSQL_PID=Developer -p 1433:1433 microsoft/mssql-server-linux:2017-latest
```