// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.ErrorHandling
{
    public class RetryErrorPolicyUseCase : UseCase
    {
        public RetryErrorPolicyUseCase()
        {
            Title = "Simulate a processing error (Retry and succeed)";
            Description = "The consumer will retry to process the message (x3) and " +
                          "finally process it.";
            ExecutionsCount = 1;
        }
    }
}
