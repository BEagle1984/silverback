---
title: Kafka deployment on Docker
permalink: /docs/extra/kafka-docker
---

## Single node basic deployment on Docker (on Windows)

Source: https://docs.confluent.io/current/installation/docker/docs/installation/single-node-client.html

### Create a Docker Network

```
docker network create confluent
```

### Start the Confluent Platform Components

#### Zookeeper

```
docker run -d --net=confluent --name=zookeeper -e ZOOKEEPER_CLIENT_PORT=2181 confluentinc/cp-zookeeper:5.0.1
```

(optional)

```
docker logs zookeeper
```

#### Kafka

```
docker run -d --net=confluent --name=kafka -e KAFKA_ZOOKEEPER_CONNECT=zookeeper:2181 -e KAFKA_ADVERTISED_LISTENERS=PLAINTEXT://kafka:9092 -e KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR=1 -p 9092:9092 confluentinc/cp-kafka:5.0.1
```

(optional)

```
docker logs kafka
```

### Important! Edit DNS settings

Add following line to ect/hosts
```
    127.0.0.1     kafka
```