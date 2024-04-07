// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

// TODO: Reimplement EventSourcing?
// // Copyright (c) 2020 Sergio Aquilini
// // This code is licensed under MIT license (see LICENSE file for details)
//
// using System.Linq;
// using System.Threading.Tasks;
// using FluentAssertions;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.DependencyInjection;
// using Silverback.Configuration;
// using Silverback.Messaging.Configuration;
// using Silverback.Messaging.Messages;
// using Silverback.Tests.Integration.E2E.TestHost;
// using Silverback.Tests.Integration.E2E.TestTypes.Database;
// using Silverback.Tests.Integration.E2E.TestTypes.Messages;
// using Xunit;
// using Xunit.Abstractions;
//
// namespace Silverback.Tests.Integration.E2E.Kafka;
//
// public class DomainEventsFixture : KafkaTestFixture
// {
//     public DomainEventsFixture(ITestOutputHelper testOutputHelper)
//         : base(testOutputHelper)
//     {
//     }
//
//     [Fact]
//     public async Task DomainEvents_ShouldBeProduced_WhenMappedToOutboundEndpoint()
//     {
//         Host.ConfigureServicesAndRun(
//             services => services
//                 .AddLogging()
//                 .AddDbContext<TestDbContext>(options => options.UseSqlite(Host.SqliteConnectionString))
//                 .AddSilverback()
// //                 .AddDelegateSubscriber<ValueChangedDomainEvent, TestEventOne>(HandleDomainEvent)
//                 .WithConnectionToMessageBroker(options => options.AddMockedKafka())
//                 .AddKafkaEndpoints(
//                     endpoints => endpoints
//                         .WithBootstrapServers("PLAINTEXT://e2e")
//                         .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName)))
//                 .AddIntegrationSpyAndSubscriber());
//
//         static TestEventOne HandleDomainEvent(ValueChangedDomainEvent domainEvent) => new() { Content = $"new value: {domainEvent.Source?.Value}" };
//
//         using (IServiceScope scope = Host.ServiceProvider.CreateScope())
//         {
//             TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
//             await dbContext.Database.EnsureCreatedAsync();
//         }
//
//         using (IServiceScope scope = Host.ServiceProvider.CreateScope())
//         {
//             TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
//             TestDomainEntity? entity = dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
//             entity.SetValue(42);
//             await dbContext.SaveChangesAsync();
//         }
//
//         Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
//         Helper.Spy.OutboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
//         Helper.Spy.OutboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("new value: 42");
//
//         using (IServiceScope scope = Host.ServiceProvider.CreateScope())
//         {
//             TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
//             TestDomainEntity? entity = dbContext.TestDomainEntities.First();
//             entity.SetValue(42000);
//             await dbContext.SaveChangesAsync();
//         }
//
//         Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
//         Helper.Spy.OutboundEnvelopes[1].Message.Should().BeOfType<TestEventOne>();
//         Helper.Spy.OutboundEnvelopes[1].Message.As<TestEventOne>().Content.Should().Be("new value: 42000");
//     }
// }
