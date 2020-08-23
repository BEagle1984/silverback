// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common;
using Silverback.Examples.Common.Data;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Deferred
{
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Invoked by test framework")]
    [SuppressMessage("", "CA1822", Justification = "Startup contract")]
    public class DeferredOutboundUseCaseStartup
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
                .WithConnectionToMessageBroker(
                    options => options
                        .AddKafka()
                        .AddDbOutboundConnector()
                        .AddDbOutboundWorker())
                .AddSingletonBehavior<CustomHeadersBehavior>()
                .AddEndpoints(
                    endpoints => endpoints
                        .AddOutbound<IIntegrationEvent>(
                            new KafkaProducerEndpoint("silverback-examples-events")
                            {
                                Configuration = new KafkaProducerConfig
                                {
                                    BootstrapServers = "PLAINTEXT://localhost:9092"
                                }
                            }));
        }

        public void Configure(ExamplesDbContext dbContext)
        {
            dbContext.Database.EnsureCreated();
        }

        [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Instantiated by Silverback")]
        [SuppressMessage("", "CA1812", Justification = "Instantiated by Silverback")]
        private class CustomHeadersBehavior : IBehavior
        {
            public async Task<IReadOnlyCollection<object>> Handle(
                IReadOnlyCollection<object> messages,
                MessagesHandler next)
            {
                foreach (var message in messages.OfType<IOutboundEnvelope>())
                {
                    message.Headers.Add(
                        "was-created",
                        DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
                }

                return await next(messages);
            }
        }
    }
}
