// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Diagnostics;

public class FatalExceptionLoggerConsumerBehaviorTests
{
    private readonly LoggerSubstitute<FatalExceptionLoggerConsumerBehavior> _loggerSubstitute;

    private readonly IConsumerLogger<FatalExceptionLoggerConsumerBehavior> _consumerLogger;

    public FatalExceptionLoggerConsumerBehaviorTests()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddLoggerSubstitute(LogLevel.Trace)
                .AddSilverback()
                .WithConnectionToMessageBroker());

        _loggerSubstitute =
            (LoggerSubstitute<FatalExceptionLoggerConsumerBehavior>)serviceProvider
                .GetRequiredService<ILogger<FatalExceptionLoggerConsumerBehavior>>();

        _consumerLogger = serviceProvider
            .GetRequiredService<IConsumerLogger<FatalExceptionLoggerConsumerBehavior>>();
    }

    [Fact]
    public async Task HandleAsync_ExceptionThrown_ExceptionLogged()
    {
        RawInboundEnvelope rawEnvelope = new(
            new byte[5],
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        try
        {
            await new FatalExceptionLoggerConsumerBehavior(_consumerLogger).HandleAsync(
                new ConsumerPipelineContext(
                    rawEnvelope,
                    Substitute.For<IConsumer>(),
                    Substitute.For<ISequenceStore>(),
                    [],
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
        RawInboundEnvelope rawEnvelope = new(
            new byte[5],
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        Func<Task> act = () => new FatalExceptionLoggerConsumerBehavior(_consumerLogger).HandleAsync(
            new ConsumerPipelineContext(
                rawEnvelope,
                Substitute.For<IConsumer>(),
                Substitute.For<ISequenceStore>(),
                [],
                Substitute.For<IServiceProvider>()),
            _ => throw new InvalidCastException()).AsTask();

        await act.Should().ThrowExactlyAsync<ConsumerPipelineFatalException>()
            .WithInnerExceptionExactly<ConsumerPipelineFatalException, InvalidCastException>();
    }
}
