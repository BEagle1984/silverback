// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Rabbit.Deferred
{
    public class OutboundWorkerUseCase : UseCase
    {
        public OutboundWorkerUseCase()
        {
            Title = "Start outbound worker";
            Description = "The outbound worker monitors the outbox table and publishes the messages to RabbitMQ.";
            ExecutionsCount = 1;
        }
    }
}
