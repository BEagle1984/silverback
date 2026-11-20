# Silverback

A .NET message bus that bridges an in-process publisher/subscriber with Kafka and MQTT.

## Language

### Broker callbacks

**Broker callback**:
A handler invoked when a broker client, or the set of all broker clients, raises a lifecycle event such as a partitions assignment, a
connection or new statistics.
_Avoid_: Hook, event handler

**Global callback**:
A broker callback for an event that concerns all broker clients together, such as their configuration or connection.

**Broker-wide callback**:
A broker callback bound to every client of one broker, Kafka or MQTT.

**Client callback**:
A broker callback bound to a single client: one Kafka consumer, one Kafka producer or one MQTT client.
_Avoid_: Bound callback, per-consumer callback

### Kafka consuming

**End of partition**:
The position after the last record currently in a partition.
_Avoid_: EOP

**Catch-up**:
Consuming the assigned partitions until each of them reaches its end of partition, to then notify that the consumer is caught up.
_Avoid_: Drain, initial load, bootstrap

**Caught up**:
A partition is caught up when its end of partition is reached and every record before it is processed; a consumer is caught up when
all its assigned partitions are.
_Avoid_: Drained, at EOF
