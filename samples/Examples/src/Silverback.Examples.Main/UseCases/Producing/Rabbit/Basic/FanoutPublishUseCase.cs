// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Rabbit.Basic
{
    public class FanoutPublishUseCase : UseCase
    {
        public FanoutPublishUseCase()
        {
            Title = "Simple publish to a fanout exchange";
            Description = "The simplest way to publish a message to a fanout exchange. The fanout exchange routes " +
                          "all messages to all queues that are bound to it.";
        }
    }
}
