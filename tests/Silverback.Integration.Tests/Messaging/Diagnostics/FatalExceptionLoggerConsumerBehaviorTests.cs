// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Diagnostics
{
    public class FatalExceptionLoggerConsumerBehaviorTests
    {
        [Fact]
        public async Task HandleAsync_ExceptionThrown_ExceptionLogged()
        {
            var logger = new LoggerSubstitute<FatalExceptionLoggerConsumerBehavior>();
            var integrationLogger = new SilverbackIntegrationLogger<FatalExceptionLoggerConsumerBehavior>(
                logger,
                new LogTemplates());

            var rawEnvelope = new RawInboundEnvelope(
                new byte[5],
                null,
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name,
                new TestOffset());

            try
            {
                await new FatalExceptionLoggerConsumerBehavior(integrationLogger).HandleAsync(
                    new ConsumerPipelineContext(
                        rawEnvelope,
                        Substitute.For<IConsumer>(),
                        Substitute.For<ISequenceStore>(),
                        Substitute.For<IServiceProvider>()),
                    _ => throw new InvalidCastException());
            }
            catch
            {
                // Ignored
            }

            logger.Received(LogLevel.Critical, typeof(InvalidCastException));
        }

        [Fact]
        public void HandleAsync_ExceptionThrown_ExceptionRethrown()
        {
            var logger = new SilverbackIntegrationLogger<FatalExceptionLoggerConsumerBehavior>(
                new LoggerSubstitute<FatalExceptionLoggerConsumerBehavior>(),
                new LogTemplates());
            var rawEnvelope = new RawInboundEnvelope(
                new byte[5],
                null,
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name,
                new TestOffset());

            Func<Task> act = () => new FatalExceptionLoggerConsumerBehavior(logger).HandleAsync(
                new ConsumerPipelineContext(
                    rawEnvelope,
                    Substitute.For<IConsumer>(),
                    Substitute.For<ISequenceStore>(),
                    Substitute.For<IServiceProvider>()),
                _ => throw new InvalidCastException());

            act.Should().ThrowExactly<InvalidCastException>();
        }
    }
}
