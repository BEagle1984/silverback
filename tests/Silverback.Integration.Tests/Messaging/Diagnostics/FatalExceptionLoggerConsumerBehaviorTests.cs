// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Sequences;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Diagnostics;

public class FatalExceptionLoggerConsumerBehaviorTests
{
    private readonly LoggerSubstitute<FatalExceptionLoggerConsumerBehavior> _loggerSubstitute;

    private readonly ISilverbackLogger<FatalExceptionLoggerConsumerBehavior> _logger;

    public FatalExceptionLoggerConsumerBehaviorTests()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddLoggerSubstitute(LogLevel.Trace)
            .AddSilverback()
            .WithConnectionToMessageBroker());

        _loggerSubstitute = (LoggerSubstitute<FatalExceptionLoggerConsumerBehavior>)serviceProvider
            .GetRequiredService<ILogger<FatalExceptionLoggerConsumerBehavior>>();

        _logger = serviceProvider.GetRequiredService<ISilverbackLogger<FatalExceptionLoggerConsumerBehavior>>();
    }

    [Fact]
    public async Task HandleAsync_ShouldLogException()
    {
        TestInboundEnvelope<object> envelope = new(
            null,
            Stream.Null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        try
        {
            await new FatalExceptionLoggerConsumerBehavior(_logger).HandleAsync(
                new ConsumerPipelineContext(
                    envelope,
                    Substitute.For<IConsumer>(),
                    Substitute.For<ISequenceStore>(),
                    [],
                    Substitute.For<IServiceProvider>()),
                (_, _) => throw new InvalidCastException(),
                CancellationToken.None);
        }
        catch
        {
            // Ignored
        }

        _loggerSubstitute.Received(LogLevel.Critical, typeof(InvalidCastException));
    }

    [Fact]
    public async Task HandleAsync_ShouldRethrow()
    {
        TestInboundEnvelope<object> envelope = new(
            null,
            Stream.Null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        Func<Task> act = () => new FatalExceptionLoggerConsumerBehavior(_logger).HandleAsync(
            new ConsumerPipelineContext(
                envelope,
                Substitute.For<IConsumer>(),
                Substitute.For<ISequenceStore>(),
                [],
                Substitute.For<IServiceProvider>()),
            (_, _) => throw new InvalidCastException(),
            CancellationToken.None).AsTask();

        Exception exception = await act.ShouldThrowAsync<ConsumerPipelineFatalException>();
        exception.InnerException.ShouldBeOfType<InvalidCastException>();
    }
}
