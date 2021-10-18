// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Diagnostics
{
    public class FatalExceptionLoggerConsumerBehaviorTests
    {
        private readonly LoggerSubstitute<FatalExceptionLoggerConsumerBehavior> _loggerSubstitute;

        private readonly IInboundLogger<FatalExceptionLoggerConsumerBehavior> _inboundLogger;

        public FatalExceptionLoggerConsumerBehaviorTests()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddLoggerSubstitute(LogLevel.Trace)
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>()));

            _loggerSubstitute =
                (LoggerSubstitute<FatalExceptionLoggerConsumerBehavior>)serviceProvider
                    .GetRequiredService<ILogger<FatalExceptionLoggerConsumerBehavior>>();

            _inboundLogger = serviceProvider
                .GetRequiredService<IInboundLogger<FatalExceptionLoggerConsumerBehavior>>();
        }

        [Fact]
        public async Task HandleAsync_ExceptionThrown_ExceptionLogged()
        {
            var rawEnvelope = new RawInboundEnvelope(
                new byte[5],
                null,
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name,
                new TestOffset());

            try
            {
                await new FatalExceptionLoggerConsumerBehavior(_inboundLogger).HandleAsync(
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

            _loggerSubstitute.Received(LogLevel.Critical, typeof(InvalidCastException));
        }

        [Fact]
        public async Task HandleAsync_ExceptionThrown_ExceptionRethrown()
        {
            var rawEnvelope = new RawInboundEnvelope(
                new byte[5],
                null,
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name,
                new TestOffset());

            Func<Task> act = () => new FatalExceptionLoggerConsumerBehavior(_inboundLogger).HandleAsync(
                new ConsumerPipelineContext(
                    rawEnvelope,
                    Substitute.For<IConsumer>(),
                    Substitute.For<ISequenceStore>(),
                    Substitute.For<IServiceProvider>()),
                _ => throw new InvalidCastException());

            await act.Should().ThrowExactlyAsync<ConsumerPipelineFatalException>()
                .WithInnerExceptionExactly<ConsumerPipelineFatalException, InvalidCastException>();
        }
    }
}
