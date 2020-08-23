// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    public class EncryptionAvroUseCase : UseCase
    {
        public EncryptionAvroUseCase()
        {
            Title = "Encryption (Avro)";
            Description = "End-to-end encrypt the message content. This sample uses Aes with a 256 bit key.";
        }
    }
}
