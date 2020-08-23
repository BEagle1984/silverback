// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    public class BroadcastUseCase : UseCase
    {
        public BroadcastUseCase()
        {
            Title = "Multiple destination endpoints";
            Description = "The same message is produced to multiple endpoints.";
            ExecutionsCount = 1;
        }
    }
}
