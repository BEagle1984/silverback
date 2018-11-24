# Single Node Basic Deployment on Docker (on Windows)

Source: https://docs.confluent.io/current/installation/docker/docs/installation/single-node-client.html

## Create a Docker Network

```
docker network create confluent
```

## Start the Confluent Platform Components

### Zookeeper

```
docker run -d --net=confluent --name=zookeeper -e ZOOKEEPER_CLIENT_PORT=2181 confluentinc/cp-zookeeper:5.0.1
```

(optional)

```
docker logs zookeeper
```

### Kafka

```
docker run -d --net=confluent --name=kafka -e KAFKA_ZOOKEEPER_CONNECT=zookeeper:2181 -e KAFKA_ADVERTISED_LISTENERS=PLAINTEXT://kafka:9092 -e KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR=1 -p 9092:9092 confluentinc/cp-kafka:5.0.1
```

(optional)

```
docker logs kafka
```

## Create Topics (optional)

```
docker run --net=confluent --rm confluentinc/cp-kafka:5.0.1 kafka-topics --create --topic Topic1 --partitions 1 --replication-factor 1 --if-not-exists --zookeeper zookeeper:2181
```

(optional)

```
docker run --net=confluent --rm confluentinc/cp-kafka:5.0.1 kafka-topics --describe --topic Topic1 --zookeeper zookeeper:2181
```

## Produce Messages

Single:
```
docker run --net=confluent --rm confluentinc/cp-kafka:5.0.1 bash -c "seq 1 | kafka-console-producer --request-required-acks 1 --broker-list kafka:9092 --topic Topic1 && echo 'Produced 1 message.'"
```

Multiple:
```
docker run --net=confluent --rm confluentinc/cp-kafka:5.0.1 bash -c "seq 42 | kafka-console-producer --request-required-acks 1 --broker-list kafka:9092 --topic Topic1 && echo 'Produced 42 messages.'"
```

## Important! Edit DNS settings

Add following line to ect/hosts
```
    127.0.0.1     kafka
```

## Start Control Center

```
docker run -d --name=control-center --net=confluent --ulimit nofile=16384:16384 -p 9021:9021 -v /tmp/control-center/data:/var/lib/confluent-control-center -e CONTROL_CENTER_ZOOKEEPER_CONNECT=zookeeper:2181 -e CONTROL_CENTER_BOOTSTRAP_SERVERS=kafka:9092 -e CONTROL_CENTER_REPLICATION_FACTOR=1 -e CONTROL_CENTER_MONITORING_INTERCEPTOR_TOPIC_PARTITIONS=1 -e CONTROL_CENTER_INTERNAL_TOPICS_PARTITIONS=1 -e CONTROL_CENTER_STREAMS_NUM_STREAM_THREADS=2 -e CONTROL_CENTER_CONNECT_CLUSTER=http://kafka-connect:8082 confluentinc/cp-enterprise-control-center:5.0.1
```

(optional)

```
docker logs control-center | grep Started
```

Launch: http://localhost:9021