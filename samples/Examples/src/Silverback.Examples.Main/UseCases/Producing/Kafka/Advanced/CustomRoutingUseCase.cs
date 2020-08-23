// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    public class CustomRoutingUseCase : UseCase
    {
        public CustomRoutingUseCase()
        {
            Title = "Dynamic custom routing";
            Description = "In this example a custom OutboundRouter is used to " +
                          "send urgent message to an additional topic.";
        }
    }
}
