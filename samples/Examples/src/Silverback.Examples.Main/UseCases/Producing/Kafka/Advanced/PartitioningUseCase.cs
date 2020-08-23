// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    public class PartitioningUseCase : UseCase
    {
        public PartitioningUseCase()
        {
            Title = "Kafka partitioning (Kafka key)";
            Description = "Silverback allows to decorate some message properties with the [KafkaKeyMember] attribute " +
                          "to be used to generate a key for the messages being sent. " +
                          "When a key is specified it will be used to ensure that all messages with the same key land " +
                          "in the same partition (this is very important because the message ordering can be enforced " +
                          "inside the same partition only and it is also a prerequisite for the compacted topics).";
        }
    }
}
