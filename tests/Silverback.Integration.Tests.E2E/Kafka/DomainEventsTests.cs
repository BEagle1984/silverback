﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Database;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class DomainEventsTests : KafkaTestFixture
    {
        public DomainEventsTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task DomainEvents_MappedToOutboundEndpoint_AutomaticallyPublished()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .AddDelegateSubscriber((ValueChangedDomainEvent domainEvent) => new TestEventOne
                        {
                            Content = $"new value: {domainEvent.Source?.Value}"
                        })
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://tests"; })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName)))
                        .AddIntegrationSpyAndSubscriber())
                .WithTestDbContext()
                .Run();

            using (var scope = Host.ServiceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
                var entity = dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
                entity.SetValue(42);
                await dbContext.SaveChangesAsync();
            }

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.OutboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
            Helper.Spy.OutboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("new value: 42");

            using (var scope = Host.ServiceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
                var entity = dbContext.TestDomainEntities.First();
                entity.SetValue(42000);
                await dbContext.SaveChangesAsync();
            }

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
            Helper.Spy.OutboundEnvelopes[1].Message.Should().BeOfType<TestEventOne>();
            Helper.Spy.OutboundEnvelopes[1].Message.As<TestEventOne>().Content.Should().Be("new value: 42000");
        }
    }
}
