// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Basic
{
    public class SimplePublishUseCase : UseCase
    {
        public SimplePublishUseCase()
        {
            Title = "Simple publish";
            Description = "The simplest way to publish a message to a topic.";
        }
    }
}
