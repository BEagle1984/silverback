// Copyright (c) 2020 Sergio Aquilini
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
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Inbound.Transaction;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling;

public class RetryErrorPolicyTests
{
    private readonly ServiceProvider _serviceProvider;

    public RetryErrorPolicyTests()
    {
        ServiceCollection services = new();

        services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddLoggerSubstitute()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>());

        _serviceProvider = services.BuildServiceProvider();

        IBroker broker = _serviceProvider.GetRequiredService<IBroker>();
        broker.ConnectAsync().Wait();
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(7, false)]
    public void CanHandle_WithDifferentFailedAttemptsCount_ReturnReflectsMaxFailedAttempts(
        int failedAttempts,
        bool expectedResult)
    {
        IErrorPolicyImplementation policy = new RetryErrorPolicy().MaxFailedAttempts(3).Build(_serviceProvider);

        MemoryStream rawMessage = new();
        MessageHeader[] headers =
        {
            new(
                DefaultMessageHeaders.FailedAttempts,
                failedAttempts.ToString(CultureInfo.InvariantCulture))
        };

        InboundEnvelope envelope = new(
            rawMessage,
            headers,
            new TestOffset(),
            TestConsumerEndpoint.GetDefault());

        bool canHandle = policy.CanHandle(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
            new InvalidOperationException("test"));

        canHandle.Should().Be(expectedResult);
    }

    [Fact]
    public async Task HandleErrorAsync_Whatever_TrueReturned()
    {
        IErrorPolicyImplementation policy = new RetryErrorPolicy().MaxFailedAttempts(3).Build(_serviceProvider);
        InboundEnvelope envelope = new(
            "hey oh!",
            new MemoryStream(),
            null,
            new TestOffset(),
            TestConsumerEndpoint.GetDefault());

        bool result = await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
            new InvalidOperationException("test"));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HandleErrorAsync_Whatever_ConsumerRolledBackAndTransactionAborted()
    {
        IErrorPolicyImplementation policy = new RetryErrorPolicy().MaxFailedAttempts(3).Build(_serviceProvider);
        InboundEnvelope envelope = new(
            "hey oh!",
            new MemoryStream(),
            null,
            new TestOffset(),
            TestConsumerEndpoint.GetDefault());

        IConsumerTransactionManager? transactionManager = Substitute.For<IConsumerTransactionManager>();

        await policy.HandleErrorAsync(
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
