// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Rabbit.Basic
{
    public class SimpleQueuePublishUseCase : UseCase
    {
        public SimpleQueuePublishUseCase()
        {
            Title = "Simple publish to a queue";
            Description = "The simplest way to publish a message to a queue (without using an exchange).";
        }
    }
}
