// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

public class RetryErrorPolicyTests
{
    private readonly TestErrorPolicyCallback _callback = new();

    private readonly ServiceProvider _serviceProvider;

    public RetryErrorPolicyTests()
    {
        ServiceCollection services = [];

        services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker()
            .AddSingletonErrorPolicyCallback(_callback);

        _serviceProvider = services.BuildServiceProvider();
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(7, false)]
    public void CanHandle_ShouldEvaluateFailedAttempts(int failedAttempts, bool expectedResult)
    {
        RetryErrorPolicy policy = new()
        {
            MaxFailedAttempts = 3
        };
        IErrorPolicyImplementation policyImplementation = policy.Build(_serviceProvider);

        MemoryStream rawMessage = new();
        MessageHeader[] headers =
        [
            new(DefaultMessageHeaders.FailedAttempts, failedAttempts.ToString(CultureInfo.InvariantCulture))
        ];

        InboundEnvelope envelope = new(
            rawMessage,
            headers,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        bool canHandle = policyImplementation.CanHandle(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
            new InvalidOperationException("test"));

        canHandle.ShouldBe(expectedResult);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(7)]
    public void CanHandle_ShouldIgnoreFailedAttempts_WhenMaxFailedAttemptsIsNotSet(int failedAttempts)
    {
        IErrorPolicyImplementation policy = new RetryErrorPolicy().Build(_serviceProvider);

        MemoryStream rawMessage = new();
        MessageHeader[] headers =
        [
            new(DefaultMessageHeaders.FailedAttempts, failedAttempts.ToString(CultureInfo.InvariantCulture))
        ];

        InboundEnvelope envelope = new(
            rawMessage,
            headers,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        bool canHandle = policy.CanHandle(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
            new InvalidOperationException("test"));

        canHandle.ShouldBe(true);
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldReturnTrue()
    {
        RetryErrorPolicy policy = new()
        {
            MaxFailedAttempts = 3
        };
        IErrorPolicyImplementation policyImplementation = policy.Build(_serviceProvider);

        InboundEnvelope envelope = new(
            "hey oh!",
            Stream.Null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        bool result = await policyImplementation.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
            new InvalidOperationException("test"));

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldRollbackOffsetAndAbortTransaction()
    {
        RetryErrorPolicy policy = new()
        {
            MaxFailedAttempts = 3
        };
        IErrorPolicyImplementation policyImplementation = policy.Build(_serviceProvider);
        InboundEnvelope envelope = new(
            "hey oh!",
            new MemoryStream(),
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        IConsumerTransactionManager? transactionManager = Substitute.For<IConsumerTransactionManager>();

        await policyImplementation.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(
                envelope,
                _serviceProvider,
                transactionManager),
            new InvalidOperationException("test"));

        await transactionManager.Received(1).RollbackAsync(
            Arg.Any<InvalidOperationException>(),
            false,
            true,
            false);
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 30)]
    [InlineData(3, 50)]
    public async Task HandleErrorAsync_ShouldApplyIncrementalDelay(int failedAttempts, int expectedDelay)
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddLoggerSubstitute(LogLevel.Trace)
            .AddSilverback()
            .WithConnectionToMessageBroker());
        LoggerSubstitute<RetryErrorPolicy> logger = (LoggerSubstitute<RetryErrorPolicy>)serviceProvider.GetRequiredService<ILogger<RetryErrorPolicy>>();
        IErrorPolicyImplementation policy = new RetryErrorPolicy
        {
            InitialDelay = TimeSpan.FromMilliseconds(10),
            DelayIncrement = TimeSpan.FromMilliseconds(20)
        }.Build(serviceProvider);
        InboundEnvelope envelope = new(
            "hey oh!",
            new MemoryStream(),
            [new MessageHeader(DefaultMessageHeaders.FailedAttempts, failedAttempts.ToString(CultureInfo.InvariantCulture))],
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "42"));

        await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, serviceProvider),
            new InvalidOperationException("test"));

        // The first retry is delayed by the initial delay and the increment is added at each subsequent retry
        logger.Received(
            LogLevel.Trace,
            null,
            $"Waiting {expectedDelay} milliseconds before retrying to process the message(s). | EndpointName: test, BrokerMessageId: 42",
            1999);
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldInvokeCallbacks()
    {
        IErrorPolicyImplementation policy = new RetryErrorPolicy().Build(_serviceProvider);
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

        _callback.Invocations.ShouldBe([nameof(IRetryErrorPolicyCallback.OnRetryingMessageAsync)]);
        _callback.Envelopes.ShouldBe([envelope]);
        _callback.Exceptions.ShouldBe([exception]);
    }
}
