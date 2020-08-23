// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.HealthCheck
{
    public class OutboundEndpointsHealthUseCase : UseCase
    {
        public OutboundEndpointsHealthUseCase()
        {
            Title = "Check outbound endpoints";
            Description = "A ping message is sent to all configured outbound endpoints to ensure that they are " +
                          "all reachable.";
            ExecutionsCount = 1;
        }
    }
}
