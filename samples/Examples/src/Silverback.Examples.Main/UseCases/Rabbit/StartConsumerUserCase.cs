// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Examples.Main.UseCases.Rabbit
{
    public class StartConsumerUserCase : ExternalUseCase
    {
        public StartConsumerUserCase()
        {
            Title = "Start RabbitMQ consumer";
            Description = "Start the Silverback.Examples.RabbitConsumer app to consume " +
                          "the test messages being produced by the other use cases.";
        }

        public override void Run()
        {
            new RabbitConsumer.RabbitConsumerApp().Start();
        }
    }
}