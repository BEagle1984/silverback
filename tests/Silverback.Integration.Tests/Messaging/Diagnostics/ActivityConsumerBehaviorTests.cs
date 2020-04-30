// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
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
        public async Task Handle_WithTraceIdHeader_NewActivityStartedAndParentIdIsSet()
        {
            var rawEnvelope = new RawInboundEnvelope(
                new byte[5],
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.TraceId, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" }
                },
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name);

            var entered = false;
            await new ActivityConsumerBehavior().Handle(
                new ConsumerPipelineContext( new[] { rawEnvelope }, null),
                null,
                (_, __) =>
                {
                    Activity.Current.Should().NotBeNull();
                    Activity.Current.ParentId.Should().Be("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
                    Activity.Current.Id.Should().StartWith("00-0af7651916cd43dd8448eb211c80319c");

                    entered = true;

                    return Task.CompletedTask;
                });

            entered.Should().BeTrue();
        }

        [Fact]
        public void Handle_WithoutActivityHeaders_NewActivityIsStarted()
        {
            var rawEnvelope = new RawInboundEnvelope(new byte[5],
                new MessageHeaderCollection
                {
                    {DefaultMessageHeaders.TraceId, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01"}
                },
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name);

            var entered = false;
            new ActivityConsumerBehavior().Handle(new ConsumerPipelineContext(new[] {rawEnvelope}, null),
                null,
                (_, __) =>
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