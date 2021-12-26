// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silverback.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Subscribers;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing;

public partial class PublisherFixture
{
    [Fact]
    [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "Test")]
    public async Task PublishAndPublishAsync_ShouldResolveOnlyNeededTypes_WhenResolvedOnceAlready()
    {
        int resolved = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            builder => builder
                .AddFakeLogger()
                .AddSilverback()
                .AddScopedSubscriber(
                    _ =>
                    {
                        resolved++;
                        return new TestServiceOne();
                    })
                .AddScopedSubscriber(
                    _ =>
                    {
                        resolved++;
                        return new TestServiceTwo();
                    }));

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IPublisher>()
                .PublishAsync(new TestCommandOne());
        }

        resolved.Should().Be(2);

        // PublishAsync
        resolved = 0;

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(new TestCommandOne());
        }

        resolved.Should().Be(1);

        // Publish
        resolved = 0;

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IPublisher>().Publish(new TestCommandOne());
        }

        resolved.Should().Be(1);

        // Publish stream
        resolved = 0;

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(new MessageStreamProvider<TestCommandOne>());
        }

        resolved.Should().Be(0);
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "Test")]
    public async Task PublishAsync_ShouldResolveOnlyNeededTypes_WhenSubscribedMethodsLoaderServiceWasExecuted()
    {
        int resolved = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            builder => builder
                .AddFakeLogger()
                .AddSilverback()
                .AddScopedSubscriber(
                    _ =>
                    {
                        resolved++;
                        return new TestServiceOne();
                    })
                .AddScopedSubscriber(
                    _ =>
                    {
                        resolved++;
                        return new TestServiceTwo();
                    }));

        await serviceProvider.GetServices<IHostedService>()
            .OfType<SubscribedMethodsLoaderService>()
            .Single()
            .StartAsync(CancellationToken.None);

        resolved.Should().Be(2);

        // PublishAsync
        resolved = 0;

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(new TestCommandOne());
        }

        resolved.Should().Be(1);

        // Publish
        resolved = 0;

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IPublisher>().Publish(new TestCommandOne());
        }

        resolved.Should().Be(1);

        // Publish stream
        resolved = 0;

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(new MessageStreamProvider<TestCommandOne>());
        }

        resolved.Should().Be(0);
    }
}
