// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    public class ProduceEmptyMessageUseCase : UseCase
    {
        public ProduceEmptyMessageUseCase()
        {
            Title = "Produce a message without a body";
            Description = "Publish a message with an empty body to a topic.";
        }
    }
}
