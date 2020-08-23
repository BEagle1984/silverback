// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Rabbit.Basic
{
    public class TopicPublishUseCase : UseCase
    {
        public TopicPublishUseCase()
        {
            Title = "Publish to a topic exchange";
            Description = "Publish some messages to a topic exchange (with different routing keys). " +
                          "The topic exchange routes according to the routing key / binding key. " +
                          "(Only the messages with routing key starting with \"interesting.*.event\" will be " +
                          "consumed.)";
        }
    }
}
