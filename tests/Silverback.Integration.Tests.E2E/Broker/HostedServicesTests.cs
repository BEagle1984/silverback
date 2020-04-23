// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.LargeMessages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Broker
{
    public class HostedServicesTests
    {
        [Fact]
        public void OutboundQueueWorkerService_ResolvedAndInitialized()
        {
            var serviceProvider = new ServiceCollection()
                .AddNullLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options
                    .AddBroker<InMemoryBroker>()
                    .AddOutboundConnector()
                    .AddOutboundWorker())
                .Services.BuildServiceProvider(new ServiceProviderOptions
                {
                    ValidateScopes = true
                });

            var hostedServices = serviceProvider.GetServices<IHostedService>();

            var outboundWorkerService = hostedServices?.OfType<OutboundQueueWorkerService>().FirstOrDefault();

            outboundWorkerService.Should().NotBeNull();
        }

        [Fact]
        public void ChunkStoreCleanerService_ResolvedAndInitialized()
        {
            var serviceProvider = new ServiceCollection()
                .AddNullLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options
                    .AddBroker<InMemoryBroker>()
                    .AddInMemoryChunkStore())
                .Services
                .BuildServiceProvider(new ServiceProviderOptions
                {
                    ValidateScopes = true
                });

            var hostedServices = serviceProvider.GetServices<IHostedService>();

            var cleanerService = hostedServices?.OfType<ChunkStoreCleanerService>().FirstOrDefault();

            cleanerService.Should().NotBeNull();
        }
    }
}