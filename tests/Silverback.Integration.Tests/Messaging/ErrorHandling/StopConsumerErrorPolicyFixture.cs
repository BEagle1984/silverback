// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Consuming.Transaction;
using Silverback.Messaging.Messages;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling;

public class StopConsumerErrorPolicyFixture
{
    private readonly ServiceProvider _serviceProvider;

    public StopConsumerErrorPolicyFixture()
    {
        ServiceCollection services = [];

        services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void CanHandle_ShouldReturnTrue()
    {
        IErrorPolicyImplementation policy = new StopConsumerErrorPolicy().Build(_serviceProvider);
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
    public async Task HandleErrorAsync_ShouldReturnFalse()
    {
        IErrorPolicyImplementation policy = new StopConsumerErrorPolicy().Build(_serviceProvider);
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

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldNotCommittedNorAbortedTransaction()
    {
        /* The consumer will be stopped and the transaction aborted by the consumer/behavior */

        IErrorPolicyImplementation policy = new StopConsumerErrorPolicy().Build(_serviceProvider);
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

        await transactionManager.ReceivedWithAnyArgs(0).CommitAsync();
        await transactionManager.ReceivedWithAnyArgs(0).RollbackAsync(Arg.Any<InvalidOperationException>());
    }
}
