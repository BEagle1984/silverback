// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Silverback.Background;
using Silverback.Examples.Common;
using Silverback.Examples.Common.Data;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;

namespace Silverback.Examples.Main.UseCases.Producing.Rabbit.Deferred
{
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Invoked by test framework")]
    [SuppressMessage("", "CA1822", Justification = "Startup contract")]
    public class OutboundWorkerUseCaseStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddDbContext<ExamplesDbContext>(
                    options => options
                        .UseSqlServer(SqlServerConnectionHelper.GetProducerConnectionString()))
                .AddSilverback()
                .UseModel()
                .UseDbContext<ExamplesDbContext>()
                .AddDbDistributedLockManager()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddRabbit()
                        .AddDbOutboundConnector()
                        .AddDbOutboundWorker(
                            new DistributedLockSettings(
                                acquireRetryInterval: TimeSpan.FromSeconds(1),
                                heartbeatTimeout: TimeSpan.FromSeconds(10),
                                heartbeatInterval: TimeSpan.FromSeconds(1))))
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

            Console.WriteLine("Starting OutboundWorker background process (press CTRL-C to stop)...");
        }

        public void Configure(ExamplesDbContext dbContext)
        {
            dbContext.Database.EnsureCreated();
        }
    }
}
