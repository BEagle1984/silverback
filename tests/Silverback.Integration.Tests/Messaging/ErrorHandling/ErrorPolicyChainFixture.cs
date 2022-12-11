// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling;

public class ErrorPolicyChainFixture
{
    private readonly IServiceProvider _serviceProvider;

    public ErrorPolicyChainFixture()
    {
        ServiceCollection services = new();

        services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(130)]
    public void CanHandle_ShouldReturnTrueRegardlessOfFailedAttempts(int failedAttempts)
    {
        MemoryStream rawMessage = new();
        MessageHeader[] headers =
        {
            new(DefaultMessageHeaders.FailedAttempts, failedAttempts.ToString(CultureInfo.InvariantCulture))
        };

        TestErrorPolicy testPolicy = new();

        IErrorPolicyImplementation chain = new ErrorPolicyChain(
                new RetryErrorPolicy
                {
                    MaxFailedAttempts = 3
                },
                testPolicy)
            .Build(_serviceProvider);

        bool result = chain.CanHandle(
            ConsumerPipelineContextHelper.CreateSubstitute(
                new InboundEnvelope(
                    rawMessage,
                    headers,
                    TestConsumerEndpoint.GetDefault(),
                    Substitute.For<IConsumer>(),
                    new TestOffset())),
            new InvalidOperationException("test"));

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task HandleErrorAsync_ShouldApplyPoliciesAccordingToMaxFailedAttempts(int failedAttempts)
    {
        MemoryStream rawMessage = new();
        MessageHeader[] headers =
        {
            new(DefaultMessageHeaders.FailedAttempts, failedAttempts.ToString(CultureInfo.InvariantCulture))
        };

        TestErrorPolicy testPolicy = new();

        IErrorPolicyImplementation chain = new ErrorPolicyChain(
                new RetryErrorPolicy
                {
                    MaxFailedAttempts = 3
                },
                testPolicy)
            .Build(_serviceProvider);

        await chain.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(
                new InboundEnvelope(
                    rawMessage,
                    headers,
                    TestConsumerEndpoint.GetDefault(),
                    Substitute.For<IConsumer>(),
                    new TestOffset())),
            new InvalidOperationException("test"));

        testPolicy.Applied.Should().Be(failedAttempts > 3);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 0)]
    [InlineData(3, 1)]
    [InlineData(4, 1)]
    [InlineData(5, 2)]
    public async Task HandleErrorAsync_ShouldApplyCorrectPolicyAccordingToMaxFailedAttempts(int failedAttempts, int expectedAppliedPolicy)
    {
        MemoryStream rawMessage = new();
        MessageHeader[] headers =
        {
            new(DefaultMessageHeaders.FailedAttempts, failedAttempts.ToString(CultureInfo.InvariantCulture))
        };

        ErrorPolicyBase[] policies =
        {
            new TestErrorPolicy { MaxFailedAttempts = 2 },
            new TestErrorPolicy { MaxFailedAttempts = 2 },
            new TestErrorPolicy { MaxFailedAttempts = 2 }
        };

        IErrorPolicyImplementation chain = new ErrorPolicyChain(policies).Build(_serviceProvider);

        await chain.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(
                new InboundEnvelope(
                    rawMessage,
                    headers,
                    TestConsumerEndpoint.GetDefault(),
                    Substitute.For<IConsumer>(),
                    new TestOffset())),
            new InvalidOperationException("test"));

        for (int i = 0; i < policies.Length; i++)
        {
            policies[i].As<TestErrorPolicy>().Applied.Should().Be(i == expectedAppliedPolicy);
        }
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldApplyCorrectPolicyIgnoringFailedAttemptsWhenMaxFailedAttemptsIsNotSet()
    {
        MemoryStream rawMessage = new();
        MessageHeader[] headers =
        {
            new(DefaultMessageHeaders.FailedAttempts, 42)
        };

        ErrorPolicyBase[] policies =
        {
            new TestErrorPolicy { MaxFailedAttempts = 2 },
            new TestErrorPolicy(),
            new TestErrorPolicy { MaxFailedAttempts = 2 }
        };

        IErrorPolicyImplementation chain = new ErrorPolicyChain(policies).Build(_serviceProvider);

        await chain.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(
                new InboundEnvelope(
                    rawMessage,
                    headers,
                    TestConsumerEndpoint.GetDefault(),
                    Substitute.For<IConsumer>(),
                    new TestOffset())),
            new InvalidOperationException("test"));

        policies[1].As<TestErrorPolicy>().Applied.Should().BeTrue();
    }
}
