// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Deferred
{
    public class DeferredOutboundUseCase : UseCase
    {
        public DeferredOutboundUseCase()
        {
            Title = "Deferred publish (DbOutbound)";
            Description = "The messages are stored into an outbox table and asynchronously published. " +
                          "An outbound worker have to be started to process the outbox table.";
        }
    }
}
