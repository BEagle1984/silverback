// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Silverback.Examples.Common.Messages;
using Silverback.Examples.Main.Menu;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    [SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Invoked by test framework")]
    public class ProduceByteArrayUseCaseRun : IAsyncRunnable
    {
        private readonly IBroker _broker;

        public ProduceByteArrayUseCaseRun(IBroker broker)
        {
            _broker = broker;
        }

        public async Task Run()
        {
            var byteArray = Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(
                    new SimpleIntegrationEvent
                    {
                        Content = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)
                    },
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All
                    }));

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
                ["use-case"] = "pre-serialized"
            };

            await producer.ProduceAsync(byteArray, headers);
        }
    }
}
