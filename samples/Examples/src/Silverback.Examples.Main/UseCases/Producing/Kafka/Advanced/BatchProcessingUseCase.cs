// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    public class BatchProcessingUseCase : UseCase
    {
        public BatchProcessingUseCase()
        {
            Title = "Batch processing (w/ multiple consumer threads)";
            Description = "Fetching and processing the messages in batch of multiple messages (5 in our case) " +
                          "to avoid micro-transactions when processing huge streams. The inbound connector is also " +
                          "configured to use 2 concurrent threads (2 instances of the Confluent.Kafka consumer).";
        }
    }
}
