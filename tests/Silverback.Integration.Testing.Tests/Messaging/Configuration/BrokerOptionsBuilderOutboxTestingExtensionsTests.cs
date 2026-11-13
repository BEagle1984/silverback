// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Lock;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Testing.Messaging.Configuration;

public class BrokerOptionsBuilderOutboxTestingExtensionsTests
{
    [Fact]
    public void BypassOutbox_ShouldThrow_WhenNotConnectedToMessageBroker()
    {
        BrokerOptionsBuilder builder = new(new SilverbackBuilder(new ServiceCollection()));

        Should.Throw<InvalidOperationException>(builder.BypassOutbox);
    }

    [Fact]
    public async Task BypassOutbox_ShouldCreateWriterOutlivingTheScope()
    {
        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic") { FriendlyName = "my-endpoint" });
        ServiceCollection services = new();
        services.AddSilverback().WithConnectionToMessageBroker(options => options.BypassOutbox());
        services.AddSingleton<IProducerCollection>(new ProducerCollection { producer });
        await using ServiceProvider serviceProvider = services.BuildServiceProvider();

        // The writer is cached by the factory, but it may be created from a scoped service provider
        IOutboxWriter writer;
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            writer = serviceProvider.GetRequiredService<OutboxWriterFactory>().GetWriter(new TestOutboxSettings(), scope.ServiceProvider);
        }

        await writer.AddAsync(new OutboxMessage([1], null, "my-endpoint"));

        producer.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IProducer.RawProduceAsync)).ShouldBe(1);
    }

    private sealed record TestOutboxSettings : OutboxSettings
    {
        public override DistributedLockSettings? GetCompatibleLockSettings() => null;
    }
}
