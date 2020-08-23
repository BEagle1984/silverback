// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    public class SameProcessUseCase : UseCase
    {
        public SameProcessUseCase()
        {
            Title = "Producer and Consumer in the same process";
            Description = "Sometimes you want to use Kafka only to asynchronously process some work load but the " +
                          "producer and consumer reside in the same process (same microservice). This has been very " +
                          "tricky with the earlier versions of Silverback but it now works naturally. " +
                          "Note: An inbound endpoint is added only to demonstrate that the feared 'mortal loop' is " +
                          "not an issue anymore.";
            ExecutionsCount = 1;
        }
    }
}
