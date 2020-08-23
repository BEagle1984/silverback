// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Examples.Main.Menu;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    [SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Invoked by test framework")]
    public class ProduceEmptyMessageUseCaseRun : IAsyncRunnable
    {
        private readonly IBroker _broker;

        public ProduceEmptyMessageUseCaseRun(IBroker broker)
        {
            _broker = broker;
        }

        public async Task Run()
        {
            var producer = _broker.GetProducer(
                new KafkaProducerEndpoint("silverback-examples-events")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092"
                    }
                });

            var headers = new MessageHeaderCollection
            {
                ["use-case"] = "empty-message"
            };

            await producer.ProduceAsync(null, headers);
        }
    }
}
