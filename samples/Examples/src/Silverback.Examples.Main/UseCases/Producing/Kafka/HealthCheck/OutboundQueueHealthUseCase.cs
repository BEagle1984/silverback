// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.HealthCheck
{
    public class OutboundQueueHealthUseCase : UseCase
    {
        public OutboundQueueHealthUseCase()
        {
            Title = "Check outbound queue";
            Description = "Check that the outbox queue length doesn't exceed the configured threshold and that " +
                          "the messages aren't older than the configure max age.";
            ExecutionsCount = 1;
        }
    }
}
