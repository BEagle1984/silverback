// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Basic
{
    public class NewtonsoftSerializerUseCase : UseCase
    {
        public NewtonsoftSerializerUseCase()
        {
            Title = "Newtonsoft serializer";
            Description =
                "Serialize and deserialize the messages using the legacy Newtonsoft.Json based serializer.";
        }
    }
}
