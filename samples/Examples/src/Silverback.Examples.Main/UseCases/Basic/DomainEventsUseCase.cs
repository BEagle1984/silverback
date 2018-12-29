// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Subscribers;

namespace Silverback.Examples.Main.UseCases.Basic
{
    // TODO: Implement
    public class DomainEventsUseCase : UseCase, ISubscriber
    {
        public DomainEventsUseCase() : base("Domain events", 100)
        {
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            throw new NotImplementedException();
        }

        protected override void Configure(IBrokerEndpointsConfigurationBuilder endpoints, IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }

        protected override Task Execute(IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }
    }
}