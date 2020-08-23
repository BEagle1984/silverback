// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Messages;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Invoked by test framework")]
    [SuppressMessage("", "CA1822", Justification = "Startup contract")]
    public class EncryptionUseCaseStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddKafka())
                .AddEndpoints(
                    endpoints => endpoints
                        .AddOutbound<IIntegrationEvent>(
                            new KafkaProducerEndpoint("silverback-examples-encrypted")
                            {
                                Configuration = new KafkaProducerConfig
                                {
                                    BootstrapServers = "PLAINTEXT://localhost:9092"
                                },
                                Encryption = new SymmetricEncryptionSettings
                                {
                                    Key = Constants.AesEncryptionKey
                                }
                            }));
        }

        public void Configure()
        {
        }
    }
}
