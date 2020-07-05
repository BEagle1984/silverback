// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Examples.Consumer;

namespace Silverback.Examples.Main.UseCases.Consuming
{
    public class StartConsumerUserCase : ExternalUseCase
    {
        public StartConsumerUserCase()
        {
            Title = "Start consumer application";
            Description = "Start the Silverback.Examples.Consumer app to consume " +
                          "the test messages being produced to Apache Kafka or RabbitMQ " +
                          "by the other use cases.";
        }

        public override void Run()
        {
            new ConsumerApp().Start();
        }
    }
}
