// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.ErrorHandling
{
    public class UnhandledErrorUseCase : UseCase
    {
        public UnhandledErrorUseCase()
        {
            Title = "Simulate a processing error without error handling policies";
            Description = "The consumer will be stopped (and then restarted after a while).";
            ExecutionsCount = 1;
        }
    }
}
