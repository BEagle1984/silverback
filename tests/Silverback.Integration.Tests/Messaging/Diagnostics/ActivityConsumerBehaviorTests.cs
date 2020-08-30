// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Diagnostics
{
    public class ActivityConsumerBehaviorTests
    {
        public ActivityConsumerBehaviorTests()
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        }

        [Fact]
        public async Task HandleAsync_WithTraceIdHeader_NewActivityStartedAndParentIdIsSet()
        {
            var rawEnvelope = new RawInboundEnvelope(
                new byte[5],
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.TraceId, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" }
                },
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name,
                new TestOffset());

            var entered = false;
            await new ActivityConsumerBehavior().HandleAsync(
                new ConsumerPipelineContext(
                    rawEnvelope,
                    Substitute.For<IConsumer>(),
                    Substitute.For<ISequenceStore>(),
                    Substitute.For<IServiceProvider>()),
                _ =>
                {
                    Activity.Current.Should().NotBeNull();
                    Activity.Current.ParentId.Should()
                        .Be("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
                    Activity.Current.Id.Should().StartWith("00-0af7651916cd43dd8448eb211c80319c");

                    entered = true;

                    return Task.CompletedTask;
                });

            entered.Should().BeTrue();
        }

        [Fact]
        public void HandleAsync_WithoutActivityHeaders_NewActivityIsStarted()
        {
            var rawEnvelope = new RawInboundEnvelope(
                new byte[5],
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.TraceId, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" }
                },
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name,
                new TestOffset());

            var entered = false;
            new ActivityConsumerBehavior().HandleAsync(
                new ConsumerPipelineContext(
                    rawEnvelope,
                    Substitute.For<IConsumer>(),
                    Substitute.For<ISequenceStore>(),
                    Substitute.For<IServiceProvider>()),
                _ =>
                {
                    Activity.Current.Should().NotBeNull();
                    Activity.Current.Id.Should().NotBeNullOrEmpty();

                    entered = true;

                    return Task.CompletedTask;
                });

            entered.Should().BeTrue();
        }
    }
}
