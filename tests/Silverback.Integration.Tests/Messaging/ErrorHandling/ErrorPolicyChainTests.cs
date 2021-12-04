// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling;

public class ErrorPolicyChainTests
{
    private readonly IServiceProvider _serviceProvider;

    public ErrorPolicyChainTests()
    {
        ServiceCollection services = new();

        services
            .AddLoggerSubstitute()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>());

        _serviceProvider = services.BuildServiceProvider();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(130)]
    public void CanHandle_Whatever_TrueReturned(int failedAttempts)
    {
        MemoryStream rawMessage = new();
        MessageHeader[] headers =
        {
            new(
                DefaultMessageHeaders.FailedAttempts,
                failedAttempts.ToString(CultureInfo.InvariantCulture))
        };

        TestErrorPolicy testPolicy = new();

        IErrorPolicyImplementation chain = new ErrorPolicyChain(
                new RetryErrorPolicy().MaxFailedAttempts(3),
                testPolicy)
            .Build(_serviceProvider);

        bool result = chain.CanHandle(
            ConsumerPipelineContextHelper.CreateSubstitute(
                new InboundEnvelope(
                    rawMessage,
                    headers,
                    new TestOffset(),
                    TestConsumerEndpoint.GetDefault())),
            new InvalidOperationException("test"));

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task HandleErrorAsync_RetryWithMaxFailedAttempts_AppliedAccordingToMaxFailedAttempts(int failedAttempts)
    {
        MemoryStream rawMessage = new();
        MessageHeader[] headers =
        {
            new(
                DefaultMessageHeaders.FailedAttempts,
                failedAttempts.ToString(CultureInfo.InvariantCulture))
        };

        TestErrorPolicy testPolicy = new();

        IErrorPolicyImplementation chain = new ErrorPolicyChain(
                new RetryErrorPolicy().MaxFailedAttempts(3),
                testPolicy)
            .Build(_serviceProvider);

        await chain.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(
                new InboundEnvelope(
                    rawMessage,
                    headers,
                    new TestOffset(),
                    TestConsumerEndpoint.GetDefault())),
            new InvalidOperationException("test"));

        testPolicy.Applied.Should().Be(failedAttempts > 3);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 0)]
    [InlineData(3, 1)]
    [InlineData(4, 1)]
    [InlineData(5, 2)]
    public async Task HandleErrorAsync_MultiplePoliciesWithMaxFailedAttempts_CorrectPolicyApplied(
        int failedAttempts,
        int expectedAppliedPolicy)
    {
        MemoryStream rawMessage = new();
        MessageHeader[] headers =
        {
            new(
                DefaultMessageHeaders.FailedAttempts,
                failedAttempts.ToString(CultureInfo.InvariantCulture))
        };

        ErrorPolicyBase[] policies =
        {
            new TestErrorPolicy().MaxFailedAttempts(2),
            new TestErrorPolicy().MaxFailedAttempts(2),
            new TestErrorPolicy().MaxFailedAttempts(2)
        };

        IErrorPolicyImplementation chain = new ErrorPolicyChain(policies)
            .Build(_serviceProvider);

        await chain.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(
                new InboundEnvelope(
                    rawMessage,
                    headers,
                    new TestOffset(),
                    TestConsumerEndpoint.GetDefault())),
            new InvalidOperationException("test"));

        for (int i = 0; i < policies.Length; i++)
        {
            policies[i].As<TestErrorPolicy>().Applied.Should().Be(i == expectedAppliedPolicy);
        }
    }
}
