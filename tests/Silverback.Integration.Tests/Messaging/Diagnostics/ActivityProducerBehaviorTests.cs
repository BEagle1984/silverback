// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
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
        public void Handle_StartedActivity_TraceIdHeaderIsSet()
        {
            var activity = new Activity("test");
            activity.SetParentId("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
            activity.Start();
            var envelope = new OutboundEnvelope(null, null, TestProducerEndpoint.GetDefault());

            new ActivityProducerBehavior().Handle(
                new ProducerPipelineContext(envelope, null),
                _ => Task.CompletedTask);

            envelope.Headers.Should().Contain(
                h => h.Key == DefaultMessageHeaders.TraceId &&
                     h.Value.StartsWith("00-0af7651916cd43dd8448eb211c80319c"));
        }

        [Fact]
        public void Handle_NoStartedActivity_ActivityStartedAndTraceIdHeaderIsSet()
        {
            var envelope = new OutboundEnvelope(null, null, TestProducerEndpoint.GetDefault());

            new ActivityProducerBehavior().Handle(
                new ProducerPipelineContext(envelope, null),
                _ => Task.CompletedTask);

            envelope.Headers.Should().Contain(
                h => h.Key == DefaultMessageHeaders.TraceId && !string.IsNullOrEmpty(h.Value));
        }

        [Fact]
        public void Handle_FromProduceWithStartedActivity_TraceIdHeaderIsSet()
        {
            var services = new ServiceCollection();
            services
                .AddNullLogger()
                .AddSilverback().WithConnectionToMessageBroker(options => options
                    .AddBroker<TestBroker>());
            var serviceProvider = services.BuildServiceProvider();
            var broker = (TestBroker) serviceProvider.GetRequiredService<IBroker>();

            var activity = new Activity("test");
            activity.SetParentId("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
            activity.Start();

            broker.GetProducer(TestProducerEndpoint.GetDefault()).Produce("123");

            broker.ProducedMessages.Single().Headers.Should().Contain(
                h => h.Key == DefaultMessageHeaders.TraceId &&
                     h.Value.StartsWith("00-0af7651916cd43dd8448eb211c80319c"));
        }

        [Fact]
        public async Task Handle_FromProduceAsyncWithStartedActivity_TraceIdHeaderIsSet()
        {
            var services = new ServiceCollection();
            services
                .AddNullLogger()
                .AddSilverback().WithConnectionToMessageBroker(options => options
                    .AddBroker<TestBroker>());
            var serviceProvider = services.BuildServiceProvider();
            var broker = (TestBroker) serviceProvider.GetRequiredService<IBroker>();

            var activity = new Activity("test");
            activity.SetParentId("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
            activity.Start();

            await broker.GetProducer(TestProducerEndpoint.GetDefault()).ProduceAsync("123");

            broker.ProducedMessages.Single().Headers.Should().Contain(
                h => h.Key == DefaultMessageHeaders.TraceId &&
                     h.Value.StartsWith("00-0af7651916cd43dd8448eb211c80319c"));
        }
    }
}