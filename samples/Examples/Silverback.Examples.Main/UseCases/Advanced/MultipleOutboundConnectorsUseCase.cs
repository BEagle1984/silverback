using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration;

namespace Silverback.Examples.Main.UseCases.Advanced
{
    // TODO: Implement
    public class MultipleOutboundConnectorsUseCase : UseCase
    {
        public MultipleOutboundConnectorsUseCase() : base("Multiple outbound connectors", 20)
        {
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            throw new NotImplementedException();
        }

        protected override void Configure(IBrokerEndpointsConfigurationBuilder endpoints)
        {
            throw new NotImplementedException();
        }

        protected override Task Execute(IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }
    }
}