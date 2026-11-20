# Silverback

A .NET message bus that bridges an in-process publisher/subscriber with Kafka and MQTT.

## Language

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
