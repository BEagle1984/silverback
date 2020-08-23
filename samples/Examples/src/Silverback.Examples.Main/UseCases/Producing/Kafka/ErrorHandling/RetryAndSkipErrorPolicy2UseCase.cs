// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.ErrorHandling
{
    public class RetryAndSkipErrorPolicy2UseCase : UseCase
    {
        public RetryAndSkipErrorPolicy2UseCase()
        {
            Title = "Simulate a deserialization error (Retry + Skip)";
            Description = "The consumer will retry to process the message (x2) and " +
                          "finally skip it.";
            ExecutionsCount = 1;
        }
    }
}
