﻿// Copyright (c) 2020 Sergio Aquilini
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

public class SkipMessageErrorPolicyFixture
{
    private readonly ServiceProvider _serviceProvider;

    public SkipMessageErrorPolicyFixture()
    {
        ServiceCollection services = new();

        services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>());

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
            new TestOffset(),
            TestConsumerEndpoint.GetDefault());

        bool canHandle = policy.CanHandle(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
            new InvalidOperationException("test"));

        canHandle.Should().BeTrue();
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldReturnTrue()
    {
        IErrorPolicyImplementation policy = new SkipMessageErrorPolicy().Build(_serviceProvider);
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
    public async Task HandleError_ShouldCommitOffsetButAbortTransaction()
    {
        IErrorPolicyImplementation policy = new SkipMessageErrorPolicy().Build(_serviceProvider);
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

        await transactionManager.Received(1).RollbackAsync(Arg.Any<InvalidOperationException>(), true);
    }
}