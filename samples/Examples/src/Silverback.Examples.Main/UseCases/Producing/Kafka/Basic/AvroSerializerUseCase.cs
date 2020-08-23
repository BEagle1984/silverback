// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Basic
{
    public class AvroSerializerUseCase : UseCase
    {
        public AvroSerializerUseCase()
        {
            Title = "Avro serializer";
            Description = "Serialize the messages in Apache Avro format using a schema registry.";
        }
    }
}
