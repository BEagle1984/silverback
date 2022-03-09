// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public class ErrorPolicyChainBuilderFixture
{
    [Fact]
    public void ThenStop_ShouldAddDefaultStopErrorPolicy()
    {
        ErrorPolicyBuilder builder = new();

        builder.Skip().ThenStop();

        IErrorPolicy policy = builder.Build();
        policy.Should().BeOfType<ErrorPolicyChain>();
        policy.As<ErrorPolicyChain>().Policies.Should().HaveCount(2);
        policy.As<ErrorPolicyChain>().Policies[0].Should().BeOfType<SkipMessageErrorPolicy>();
        policy.As<ErrorPolicyChain>().Policies[1].Should().BeOfType<StopConsumerErrorPolicy>();
        policy.As<ErrorPolicyChain>().Policies[1].Should().BeEquivalentTo(new StopConsumerErrorPolicy());
    }

    [Fact]
    public void ThenStop_ShouldAddStopErrorPolicyWithOptions()
    {
        ErrorPolicyBuilder builder = new();

        builder.Skip().ThenStop(stopPolicy => stopPolicy.Exclude<InvalidOperationException>());

        IErrorPolicy policy = builder.Build();
        policy.Should().BeOfType<ErrorPolicyChain>();
        policy.As<ErrorPolicyChain>().Policies.Should().HaveCount(2);
        policy.As<ErrorPolicyChain>().Policies[0].Should().BeOfType<SkipMessageErrorPolicy>();
        policy.As<ErrorPolicyChain>().Policies[1].Should().BeOfType<StopConsumerErrorPolicy>();
        policy.As<ErrorPolicyChain>().Policies[1].As<StopConsumerErrorPolicy>().ExcludedExceptions.Should()
            .BeEquivalentTo(new[] { typeof(InvalidOperationException) });
    }

    [Fact]
    public void ThenSkip_ShouldAddDefaultSkipMessageErrorPolicy()
    {
        ErrorPolicyBuilder builder = new();

        builder.Skip().ThenSkip();

        IErrorPolicy policy = builder.Build();
        policy.Should().BeOfType<ErrorPolicyChain>();
        policy.As<ErrorPolicyChain>().Policies.Should().HaveCount(2);
        policy.As<ErrorPolicyChain>().Policies[0].Should().BeOfType<SkipMessageErrorPolicy>();
        policy.As<ErrorPolicyChain>().Policies[1].Should().BeOfType<SkipMessageErrorPolicy>();
        policy.As<ErrorPolicyChain>().Policies[1].Should().BeEquivalentTo(new StopConsumerErrorPolicy());
    }

    [Fact]
    public void ThenSkip_ShouldAddSkipMessageErrorPolicyWithOptions()
    {
        ErrorPolicyBuilder builder = new();

        builder.Skip().ThenSkip(policy => policy.ApplyTo<TimeoutException>());

        IErrorPolicy policy = builder.Build();
        policy.Should().BeOfType<ErrorPolicyChain>();
        policy.As<ErrorPolicyChain>().Policies.Should().HaveCount(2);
        policy.As<ErrorPolicyChain>().Policies[0].Should().BeOfType<SkipMessageErrorPolicy>();
        policy.As<ErrorPolicyChain>().Policies[1].Should().BeOfType<SkipMessageErrorPolicy>();
        policy.As<ErrorPolicyChain>().Policies[1].As<SkipMessageErrorPolicy>().IncludedExceptions.Should().BeEquivalentTo(new[] { typeof(TimeoutException) });
    }

    [Fact]
    public void ThenRetry_ShouldAddDefaultRetryErrorPolicy()
    {
        ErrorPolicyBuilder builder = new();

        builder.Skip().ThenRetry();

        IErrorPolicy policy = builder.Build();
        policy.Should().BeOfType<ErrorPolicyChain>();
        policy.As<ErrorPolicyChain>().Policies.Should().HaveCount(2);
        policy.As<ErrorPolicyChain>().Policies[0].Should().BeOfType<SkipMessageErrorPolicy>();
        policy.As<ErrorPolicyChain>().Policies[1].Should().BeOfType<RetryErrorPolicy>();
        policy.As<ErrorPolicyChain>().Policies[1].Should().BeEquivalentTo(new RetryErrorPolicy());
    }

    [Fact]
    public void ThenRetry_ShouldAddRetryErrorPolicyWithOptions()
    {
        ErrorPolicyBuilder builder = new();

        builder.Skip().ThenRetry(policy => policy.WithInterval(TimeSpan.FromDays(42)));
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<ErrorPolicyChain>();
        policy.As<ErrorPolicyChain>().Policies.Should().HaveCount(2);
        policy.As<ErrorPolicyChain>().Policies[0].Should().BeOfType<SkipMessageErrorPolicy>();
        policy.As<ErrorPolicyChain>().Policies[1].Should().BeOfType<RetryErrorPolicy>();
        policy.As<ErrorPolicyChain>().Policies[1].As<RetryErrorPolicy>().InitialDelay.Should().Be(TimeSpan.FromDays(42));
    }

    [Fact]
    public void ThenRetry_ShouldAddRetryErrorPolicyWithRetriesCount()
    {
        ErrorPolicyBuilder builder = new();

        builder.Skip().ThenRetry(42);
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<ErrorPolicyChain>();
        policy.As<ErrorPolicyChain>().Policies.Should().HaveCount(2);
        policy.As<ErrorPolicyChain>().Policies[0].Should().BeOfType<SkipMessageErrorPolicy>();
        policy.As<ErrorPolicyChain>().Policies[1].Should().BeOfType<RetryErrorPolicy>();
        policy.As<ErrorPolicyChain>().Policies[1].As<RetryErrorPolicy>().MaxFailedAttempts.Should().Be(42);
    }

    [Fact]
    public void ThenRetry_ShouldAddRetryErrorPolicyWithRetriesCountAndOptions()
    {
        ErrorPolicyBuilder builder = new();

        builder.Skip().ThenRetry(42, policy => policy.WithInterval(TimeSpan.FromDays(42)));
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<ErrorPolicyChain>();
        policy.As<ErrorPolicyChain>().Policies.Should().HaveCount(2);
        policy.As<ErrorPolicyChain>().Policies[0].Should().BeOfType<SkipMessageErrorPolicy>();
        policy.As<ErrorPolicyChain>().Policies[1].Should().BeOfType<RetryErrorPolicy>();
        policy.As<ErrorPolicyChain>().Policies[1].As<RetryErrorPolicy>().MaxFailedAttempts.Should().Be(42);
        policy.As<ErrorPolicyChain>().Policies[1].As<RetryErrorPolicy>().InitialDelay.Should().Be(TimeSpan.FromDays(42));
    }

    [Fact]
    public void ThenMove_ShouldAddMoveMessageErrorPolicy()
    {
        ErrorPolicyBuilder builder = new();
        TestProducerConfiguration producerConfiguration = TestProducerConfiguration.GetDefault();

        builder.Skip().ThenMove(producerConfiguration);
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<ErrorPolicyChain>();
        policy.As<ErrorPolicyChain>().Policies.Should().HaveCount(2);
        policy.As<ErrorPolicyChain>().Policies[0].Should().BeOfType<SkipMessageErrorPolicy>();
        policy.As<ErrorPolicyChain>().Policies[1].Should().BeOfType<MoveMessageErrorPolicy>();
        policy.As<ErrorPolicyChain>().Policies[1].As<MoveMessageErrorPolicy>().ProducerConfiguration.Should().BeSameAs(producerConfiguration);
    }

    [Fact]
    public void ThenMove_ShouldAddMoveMessageErrorPolicyWithOptions()
    {
        ErrorPolicyBuilder builder = new();
        TestProducerConfiguration producerConfiguration = TestProducerConfiguration.GetDefault();

        builder.Skip().ThenMove(producerConfiguration, policy => policy.WithMaxRetries(42));
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<ErrorPolicyChain>();
        policy.As<ErrorPolicyChain>().Policies.Should().HaveCount(2);
        policy.As<ErrorPolicyChain>().Policies[0].Should().BeOfType<SkipMessageErrorPolicy>();
        policy.As<ErrorPolicyChain>().Policies[1].Should().BeOfType<MoveMessageErrorPolicy>();
        policy.As<ErrorPolicyChain>().Policies[1].As<MoveMessageErrorPolicy>().ProducerConfiguration.Should().BeSameAs(producerConfiguration);
        policy.As<ErrorPolicyChain>().Policies[1].As<MoveMessageErrorPolicy>().MaxFailedAttempts.Should().Be(42);
    }
}
