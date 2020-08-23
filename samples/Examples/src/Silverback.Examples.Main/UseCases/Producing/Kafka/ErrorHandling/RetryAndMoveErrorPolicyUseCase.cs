// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.ErrorHandling
{
    public class RetryAndMoveErrorPolicyUseCase : UseCase
    {
        public RetryAndMoveErrorPolicyUseCase()
        {
            Title = "Simulate a processing error (Retry + Move)";
            Description = "The consumer will retry to process the message (x2), " +
                          "then move it at the end of the topic (x2) and finally " +
                          "move it to another topic.";
            ExecutionsCount = 1;
        }
    }
}
