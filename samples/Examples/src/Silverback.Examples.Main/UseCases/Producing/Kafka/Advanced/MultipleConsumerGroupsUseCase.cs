// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    public class MultipleConsumerGroupsUseCase : UseCase
    {
        public MultipleConsumerGroupsUseCase()
        {
            Title = "Multiple consumer groups in the same process";
            Description =
                "The messages are published in the simplest way possible but in the consumer app there will be" +
                "multiple consumers listening to the message.";
            ExecutionsCount = 1;
        }
    }
}
