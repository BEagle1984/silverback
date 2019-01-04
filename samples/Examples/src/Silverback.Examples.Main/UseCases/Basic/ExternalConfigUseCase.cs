// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Main.UseCases.Basic
{
    public class ExternalConfigUseCase : UseCase
    {
        public ExternalConfigUseCase() : base("External configuration", 1000)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
        }

        public IConfiguration Configuration { get; }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddBus()
            .AddBroker<KafkaBroker>();

        protected override void Configure(IBrokerEndpointsConfigurationBuilder endpoints, IServiceProvider serviceProvider) => endpoints
            .ReadConfig(Configuration, serviceProvider)
            .Broker.Connect();

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var publisher = serviceProvider.GetService<IEventPublisher>();

            await publisher.PublishAsync(new SimpleIntegrationEvent { Content = DateTime.Now.ToString("HH:mm:ss.fff") });
        }
    }
}