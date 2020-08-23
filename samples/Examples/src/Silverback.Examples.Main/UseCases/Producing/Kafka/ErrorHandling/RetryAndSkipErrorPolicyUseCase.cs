// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.ErrorHandling
{
    public class RetryAndSkipErrorPolicyUseCase : UseCase
    {
        public RetryAndSkipErrorPolicyUseCase()
        {
            Title = "Simulate a processing error (Retry + Skip)";
            Description = "The consumer will retry to process the message (x2) and " +
                          "finally skip it.";
            ExecutionsCount = 1;
        }
    }
}
