// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;

namespace Silverback.Examples.Main.UseCases.Producing.Rabbit.Basic
{
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Invoked by test framework")]
    [SuppressMessage("", "CA1822", Justification = "Startup contract")]
    public class FanoutPublishUseCaseStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddRabbit())
                .AddEndpoints(
                    endpoints => endpoints
                        .AddOutbound<IIntegrationEvent>(
                            new RabbitExchangeProducerEndpoint("silverback-examples-events-fanout")
                            {
                                Exchange = new RabbitExchangeConfig
                                {
                                    IsDurable = true,
                                    IsAutoDeleteEnabled = false,
                                    ExchangeType = ExchangeType.Fanout
                                },
                                Connection = new RabbitConnectionConfig
                                {
                                    HostName = "localhost",
                                    UserName = "guest",
                                    Password = "guest"
                                }
                            }));
        }

        public void Configure()
        {
        }
    }
}
