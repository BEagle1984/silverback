// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    public class ProduceByteArrayUseCase : UseCase
    {
        public ProduceByteArrayUseCase()
        {
            Title = "Produce a raw byte array";
            Description = "Publish a pre-serialized message to a topic.";
        }
    }
}
