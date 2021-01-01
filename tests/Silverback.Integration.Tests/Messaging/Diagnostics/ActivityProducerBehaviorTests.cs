// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Diagnostics
{
    public class ActivityProducerBehaviorTests
    {
        public ActivityProducerBehaviorTests()
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        }

        [Fact]
        public async Task HandleAsync_StartedActivity_TraceIdHeaderIsSet()
        {
            var activity = new Activity("test");
            activity.SetParentId("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
            activity.Start();
            var envelope = new OutboundEnvelope(null, null, TestProducerEndpoint.GetDefault());

            await new ActivityProducerBehavior().HandleAsync(
                new ProducerPipelineContext(envelope, Substitute.For<IProducer>(), Substitute.For<IServiceProvider>()),
                _ => Task.CompletedTask);

            envelope.Headers.Should().Contain(
                header =>
                    header.Name == DefaultMessageHeaders.TraceId &&
                    header.Value != null &&
                    header.Value.StartsWith("00-0af7651916cd43dd8448eb211c80319c", StringComparison.Ordinal));
        }

        [Fact]
        public async Task HandleAsync_NoStartedActivity_ActivityStartedAndTraceIdHeaderIsSet()
        {
            var envelope = new OutboundEnvelope(null, null, TestProducerEndpoint.GetDefault());

            await new ActivityProducerBehavior().HandleAsync(
                new ProducerPipelineContext(envelope, Substitute.For<IProducer>(), Substitute.For<IServiceProvider>()),
                _ => Task.CompletedTask);

            envelope.Headers.Should().Contain(
                header => header.Name == DefaultMessageHeaders.TraceId && !string.IsNullOrEmpty(header.Value));
        }

        [Fact]
        public void HandleAsync_FromProduceWithStartedActivity_TraceIdHeaderIsSet()
        {
            var services = new ServiceCollection();
            services
                .AddLoggerSubstitute()
                .AddSilverback().WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>());
            var serviceProvider = services.BuildServiceProvider();
            var broker = serviceProvider.GetRequiredService<TestBroker>();

            var activity = new Activity("test");
            activity.SetParentId("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
            activity.Start();

            broker.GetProducer(TestProducerEndpoint.GetDefault()).Produce("123");

            broker.ProducedMessages.Single().Headers.Should().Contain(
                header =>
                    header.Name == DefaultMessageHeaders.TraceId &&
                    header.Value != null &&
                    header.Value.StartsWith("00-0af7651916cd43dd8448eb211c80319c", StringComparison.Ordinal));
        }

        [Fact]
        public async Task HandleAsync_FromProduceAsyncWithStartedActivity_TraceIdHeaderIsSet()
        {
            var services = new ServiceCollection();
            services
                .AddLoggerSubstitute()
                .AddSilverback().WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>());
            var serviceProvider = services.BuildServiceProvider();
            var broker = serviceProvider.GetRequiredService<TestBroker>();

            var activity = new Activity("test");
            activity.SetParentId("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
            activity.Start();

            await broker.GetProducer(TestProducerEndpoint.GetDefault()).ProduceAsync("123");

            broker.ProducedMessages.Single().Headers.Should().Contain(
                header =>
                    header.Name == DefaultMessageHeaders.TraceId &&
                    header.Value != null &&
                    header.Value.StartsWith("00-0af7651916cd43dd8448eb211c80319c", StringComparison.Ordinal));
        }
    }
}
