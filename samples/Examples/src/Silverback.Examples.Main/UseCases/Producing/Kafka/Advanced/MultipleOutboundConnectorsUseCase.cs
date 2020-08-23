// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    public class MultipleOutboundConnectorsUseCase : UseCase
    {
        public MultipleOutboundConnectorsUseCase()
        {
            Title = "Using multiple outbound connector types";
            Description = "Silverback allows to register multiple outbound/inbound connector implementations and " +
                          "choose which strategy (e.g. direct or deferred) to be applied for each endpoint.";
        }
    }
}
