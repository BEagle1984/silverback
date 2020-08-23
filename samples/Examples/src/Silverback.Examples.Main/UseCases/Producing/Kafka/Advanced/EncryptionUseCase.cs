// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    public class EncryptionUseCase : UseCase
    {
        public EncryptionUseCase()
        {
            Title = "Encryption (JSON)";
            Description = "End-to-end encrypt the message content. This sample uses Aes with a 256 bit key.";
        }
    }
}
