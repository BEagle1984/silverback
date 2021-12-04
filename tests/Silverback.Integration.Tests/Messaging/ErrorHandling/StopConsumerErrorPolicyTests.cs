// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Configuration;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Inbound.Transaction;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling;

public class StopConsumerErrorPolicyTests
{
    // ReSharper disable once NotAccessedField.Local
    private readonly ServiceProvider _serviceProvider;

    public StopConsumerErrorPolicyTests()
    {
        ServiceCollection services = new();

        services
            .AddLoggerSubstitute()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>());

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void CanHandle_Whatever_TrueReturned()
    {
        IErrorPolicyImplementation policy = new StopConsumerErrorPolicy().Build(_serviceProvider);
        InboundEnvelope envelope = new(
            "hey oh!",
            new MemoryStream(),
            null,
            new TestOffset(),
            TestConsumerEndpoint.GetDefault());

        bool canHandle = policy.CanHandle(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
            new InvalidOperationException("test"));

        canHandle.Should().BeTrue();
    }

    [Fact]
    public async Task HandleErrorAsync_Whatever_FalseReturned()
    {
        IErrorPolicyImplementation policy = new StopConsumerErrorPolicy().Build(_serviceProvider);
        InboundEnvelope envelope = new(
            "hey oh!",
            new MemoryStream(),
            null,
            new TestOffset(),
            TestConsumerEndpoint.GetDefault());

        bool result = await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
            new InvalidOperationException("test"));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleErrorAsync_Whatever_TransactionNotCommittedNorAborted()
    {
        /* The consumer will be stopped and the transaction aborted by the consumer/behavior */

        IErrorPolicyImplementation policy = new StopConsumerErrorPolicy().Build(_serviceProvider);
        InboundEnvelope envelope = new(
            "hey oh!",
            new MemoryStream(Encoding.UTF8.GetBytes("hey oh!")),
            null,
            new TestOffset(),
            new TestConsumerConfiguration("source-endpoint").GetDefaultEndpoint());

        IConsumerTransactionManager? transactionManager = Substitute.For<IConsumerTransactionManager>();

        await policy.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider, transactionManager),
            new InvalidOperationException("test"));

        await transactionManager.ReceivedWithAnyArgs(0).RollbackAsync(Arg.Any<InvalidOperationException>());
    }
}
