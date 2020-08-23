// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    public class BinaryFileUseCase : UseCase
    {
        public BinaryFileUseCase()
        {
            Title = "Publish raw binary file";
            Description = "Use a BinaryFileMessage to publish a raw binary.";
        }
    }
}
