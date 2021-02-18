// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Subscribers;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing
{
    public class PublisherCacheTests
    {
        [Fact]
        [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "Test")]
        public async Task PublishAsync_MultipleSubscriberClasses_OnlyNeededTypesResolvedAfterFirstPublish()
        {
            var resolved = 0;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
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

            using (var scope = serviceProvider.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<IPublisher>()
                    .PublishAsync(new TestCommandOne());
            }

            resolved.Should().Be(2);

            // Publish single message
            resolved = 0;

            using (var scope = serviceProvider.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<IPublisher>()
                    .PublishAsync(new TestCommandOne());
            }

            resolved.Should().Be(1);

            // Publish enumerable of messages
            resolved = 0;

            using (var scope = serviceProvider.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<IPublisher>()
                    .PublishAsync(new TestCommandOne());
            }

            resolved.Should().Be(1);

            // Publish stream
            resolved = 0;

            using (var scope = serviceProvider.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<IPublisher>()
                    .PublishAsync(new[] { new MessageStreamProvider<TestCommandOne>() });
            }

            resolved.Should().Be(0);
        }

        [Fact]
        [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "Test")]
        public async Task PublishAsync_MultipleSubscriberClasses_OnlyNeededTypesResolved()
        {
            var resolved = 0;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
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

            // Publish single message
            resolved = 0;

            using (var scope = serviceProvider.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<IPublisher>()
                    .PublishAsync(new TestCommandOne());
            }

            resolved.Should().Be(1);

            // Publish enumerable of messages
            resolved = 0;

            using (var scope = serviceProvider.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<IPublisher>()
                    .PublishAsync(new TestCommandOne());
            }

            resolved.Should().Be(1);

            // Publish stream
            resolved = 0;

            using (var scope = serviceProvider.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<IPublisher>()
                    .PublishAsync(new MessageStreamProvider<TestCommandOne>());
            }

            resolved.Should().Be(0);
        }
    }
}
