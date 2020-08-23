// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    public class ChunkingUseCase : UseCase
    {
        public ChunkingUseCase()
        {
            Title = "Message chunking";
            Description = "The messages are split into smaller messages (50 bytes each in this example) and " +
                          "transparently rebuilt on the consumer end to be consumed like usual.";
        }
    }
}
