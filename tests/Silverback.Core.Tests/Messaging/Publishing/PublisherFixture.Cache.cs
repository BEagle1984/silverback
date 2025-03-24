// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing;

public partial class PublisherFixture
{
    [Fact]
    [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "Test")]
    public async Task PublishAndPublishAsync_ShouldResolveMatchingTypesOnly()
    {
        int resolved = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            builder => builder
                .AddFakeLogger()
                .AddSilverback()
                .AddScopedSubscriber(
                    _ =>
                    {
                        resolved++;
                        return new TestSubscriber<ICommand>();
                    })
                .AddScopedSubscriber(
                    _ =>
                    {
                        resolved++;
                        return new TestSubscriber<IEvent>();
                    }));

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(new TestCommandOne());
        }

        resolved.ShouldBe(1);

        // PublishAsync
        resolved = 0;

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(new TestCommandOne());
        }

        resolved.ShouldBe(1);

        // Publish
        resolved = 0;

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IPublisher>().Publish(new TestCommandOne());
        }

        resolved.ShouldBe(1);

        // Publish stream
        resolved = 0;

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(new MessageStreamProvider<TestCommandOne>());
        }

        resolved.ShouldBe(0);
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "Test")]
    public async Task PublishAsync_ShouldResolveMatchingTypesOnly_WhenSubscribedMethodsLoaderServiceWasExecuted()
    {
        int resolved = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            builder => builder
                .AddFakeLogger()
                .AddSilverback()
                .AddScopedSubscriber(
                    _ =>
                    {
                        resolved++;
                        return new TestSubscriber<ICommand>();
                    })
                .AddScopedSubscriber(
                    _ =>
                    {
                        resolved++;
                        return new TestSubscriber<IEvent>();
                    }));

        await serviceProvider.GetServices<IHostedService>()
            .OfType<SubscribedMethodsLoaderService>()
            .Single()
            .StartAsync(CancellationToken.None);

        resolved.ShouldBe(0);

        // PublishAsync
        resolved = 0;

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(new TestCommandOne());
        }

        resolved.ShouldBe(1);

        // Publish
        resolved = 0;

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IPublisher>().Publish(new TestCommandOne());
        }

        resolved.ShouldBe(1);

        // Publish stream
        resolved = 0;

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(new MessageStreamProvider<TestCommandOne>());
        }

        resolved.ShouldBe(0);
    }
}
