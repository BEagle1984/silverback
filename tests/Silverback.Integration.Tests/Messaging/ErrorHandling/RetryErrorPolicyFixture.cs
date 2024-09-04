// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Consuming.Transaction;
using Silverback.Messaging.Messages;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling;

public class RetryErrorPolicyFixture
{
    private readonly ServiceProvider _serviceProvider;

    public RetryErrorPolicyFixture()
    {
        ServiceCollection services = [];

        services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker();

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

        canHandle.Should().Be(expectedResult);
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

        canHandle.Should().Be(true);
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

        result.Should().BeTrue();
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
}
