// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Consuming.Transaction;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling;

public class SkipMessageErrorPolicyTests
{
    private readonly TestErrorPolicyCallback _callback = new();

    private readonly ServiceProvider _serviceProvider;

    public SkipMessageErrorPolicyTests()
    {
        ServiceCollection services = [];

        services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker()
            .AddSingletonErrorPolicyCallback(_callback);

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void CanHandle_ShouldReturnTrue()
    {
        IErrorPolicyImplementation policy = new SkipMessageErrorPolicy().Build(_serviceProvider);
        InboundEnvelope envelope = new(
            "hey oh!",
            new MemoryStream(),
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        bool canHandle = policy.CanHandle(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
            new InvalidOperationException("test"));

        canHandle.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldReturnTrue()
    {
        IErrorPolicyImplementation policy = new SkipMessageErrorPolicy().Build(_serviceProvider);
        InboundEnvelope envelope = new(
            "hey oh!",
            new MemoryStream(),
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        bool result = await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
            new InvalidOperationException("test"));

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleError_ShouldCommitOffsetButAbortTransaction()
    {
        IErrorPolicyImplementation policy = new SkipMessageErrorPolicy().Build(_serviceProvider);
        InboundEnvelope envelope = new(
            "hey oh!",
            new MemoryStream(Encoding.UTF8.GetBytes("hey oh!")),
            null,
            new TestConsumerEndpointConfiguration("source-endpoint").GetDefaultEndpoint(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        IConsumerTransactionManager? transactionManager = Substitute.For<IConsumerTransactionManager>();

        await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider, transactionManager),
            new InvalidOperationException("test"));

        await transactionManager.Received(1).RollbackAsync(Arg.Any<InvalidOperationException>(), true);
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldLogAndTriggerReconnect_WhenRollbackFails()
    {
        ServiceCollection services = [];
        services
            .AddLoggerSubstitute(LogLevel.Trace)
            .AddSilverback()
            .WithConnectionToMessageBroker();
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        LoggerSubstitute<SkipMessageErrorPolicy> logger =
            (LoggerSubstitute<SkipMessageErrorPolicy>)serviceProvider.GetRequiredService<ILogger<SkipMessageErrorPolicy>>();
        IErrorPolicyImplementation policy = new SkipMessageErrorPolicy().Build(serviceProvider);
        IConsumer consumer = Substitute.For<IConsumer>();
        InboundEnvelope envelope = new(
            "hey oh!",
            new MemoryStream(),
            null,
            TestConsumerEndpoint.GetDefault(),
            consumer,
            new TestOffset("a", "42"));
        IConsumerTransactionManager transactionManager = Substitute.For<IConsumerTransactionManager>();
        transactionManager.RollbackAsync(Arg.Any<Exception?>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>())
            .Returns(Task.FromException<bool>(new InvalidOperationException("Rollback failed.")));

        bool result = await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, serviceProvider, transactionManager, consumer),
            new InvalidOperationException("test"));

        // The message can't be skipped without rolling back and committing: the consumer is reconnected instead
        result.ShouldBeTrue();
        await consumer.Received(1).TriggerReconnectAsync();
        logger.Received(LogLevel.Warning, typeof(InvalidOperationException), eventId: 1062, exceptionMessage: "Rollback failed.");
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldNotTriggerReconnect_WhenRollbackSucceeds()
    {
        IErrorPolicyImplementation policy = new SkipMessageErrorPolicy().Build(_serviceProvider);
        IConsumer consumer = Substitute.For<IConsumer>();
        InboundEnvelope envelope = new(
            "hey oh!",
            new MemoryStream(),
            null,
            TestConsumerEndpoint.GetDefault(),
            consumer,
            new TestOffset());

        bool result = await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider, consumer: consumer),
            new InvalidOperationException("test"));

        result.ShouldBeTrue();
        await consumer.DidNotReceive().TriggerReconnectAsync();
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldInvokeCallbacks()
    {
        IErrorPolicyImplementation policy = new SkipMessageErrorPolicy().Build(_serviceProvider);
        InboundEnvelope envelope = new(
            "hey oh!",
            new MemoryStream(),
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());
        InvalidOperationException exception = new("test");

        await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
            exception);

        _callback.Invocations.ShouldBe([nameof(ISkipMessageErrorPolicyCallback.OnSkippingMessageAsync)]);
        _callback.Envelopes.ShouldBe([envelope]);
        _callback.Exceptions.ShouldBe([exception]);
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldInvokeCallbacksBeforeSkipping()
    {
        IConsumerTransactionManager transactionManager = Substitute.For<IConsumerTransactionManager>();
        bool? rolledBackWhenInvoked = null;
        ISkipMessageErrorPolicyCallback callback = Substitute.For<ISkipMessageErrorPolicyCallback>();
        callback.OnSkippingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>()).Returns(_ =>
        {
            rolledBackWhenInvoked = transactionManager.ReceivedCalls().Any();
            return Task.CompletedTask;
        });
        ServiceProvider serviceProvider = BuildServiceProvider(callback);

        IErrorPolicyImplementation policy = new SkipMessageErrorPolicy().Build(serviceProvider);
        InboundEnvelope envelope = new(
            "hey oh!",
            new MemoryStream(),
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, serviceProvider, transactionManager),
            new InvalidOperationException("test"));

        rolledBackWhenInvoked.ShouldBe(false);
        await transactionManager.Received(1).RollbackAsync(Arg.Any<InvalidOperationException>(), true);
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldThrowAndNotSkip_WhenCallbackThrows()
    {
        IConsumerTransactionManager transactionManager = Substitute.For<IConsumerTransactionManager>();
        ISkipMessageErrorPolicyCallback callback = Substitute.For<ISkipMessageErrorPolicyCallback>();
        callback.OnSkippingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(Task.FromException(new InvalidOperationException("Callback failed.")));
        ServiceProvider serviceProvider = BuildServiceProvider(callback);

        IErrorPolicyImplementation policy = new SkipMessageErrorPolicy().Build(serviceProvider);
        InboundEnvelope envelope = new(
            "hey oh!",
            new MemoryStream(),
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        Func<Task> act = () => policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, serviceProvider, transactionManager),
            new InvalidOperationException("test"));

        ErrorPolicyCallbackInvocationException exception = await act.ShouldThrowAsync<ErrorPolicyCallbackInvocationException>();
        exception.InnerException.ShouldBeOfType<InvalidOperationException>();
        transactionManager.ReceivedCalls().ShouldBeEmpty();
    }

    private static ServiceProvider BuildServiceProvider(ISkipMessageErrorPolicyCallback callback)
    {
        ServiceCollection services = [];

        services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker()
            .AddSingletonErrorPolicyCallback(callback);

        return services.BuildServiceProvider();
    }
}
