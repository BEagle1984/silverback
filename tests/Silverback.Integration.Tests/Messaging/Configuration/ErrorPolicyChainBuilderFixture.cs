// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.ErrorHandling;
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
        ErrorPolicyChain chain = policy.ShouldBeOfType<ErrorPolicyChain>();
        chain.Policies.Count.ShouldBe(2);
        chain.Policies[0].ShouldBeOfType<SkipMessageErrorPolicy>();
        chain.Policies[1].ShouldBeEquivalentTo(new StopConsumerErrorPolicy());
    }

    [Fact]
    public void ThenStop_ShouldAddStopErrorPolicyWithOptions()
    {
        ErrorPolicyBuilder builder = new();

        builder.Skip().ThenStop(stopPolicy => stopPolicy.Exclude<InvalidOperationException>());

        IErrorPolicy policy = builder.Build();
        ErrorPolicyChain chain = policy.ShouldBeOfType<ErrorPolicyChain>();
        chain.Policies.Count.ShouldBe(2);
        chain.Policies[0].ShouldBeOfType<SkipMessageErrorPolicy>();
        chain.Policies[1].ShouldBeOfType<StopConsumerErrorPolicy>();
        StopConsumerErrorPolicy stopConsumerErrorPolicy = chain.Policies[1].ShouldBeOfType<StopConsumerErrorPolicy>();
        stopConsumerErrorPolicy.ExcludedExceptions.ShouldBe([typeof(InvalidOperationException)]);
    }

    [Fact]
    public void ThenSkip_ShouldAddDefaultSkipMessageErrorPolicy()
    {
        ErrorPolicyBuilder builder = new();

        builder.Skip().ThenSkip();

        IErrorPolicy policy = builder.Build();
        ErrorPolicyChain chain = policy.ShouldBeOfType<ErrorPolicyChain>();
        chain.Policies.Count.ShouldBe(2);
        chain.Policies[0].ShouldBeOfType<SkipMessageErrorPolicy>();
        chain.Policies[1].ShouldBeEquivalentTo(new SkipMessageErrorPolicy());
    }

    [Fact]
    public void ThenSkip_ShouldAddSkipMessageErrorPolicyWithOptions()
    {
        ErrorPolicyBuilder builder = new();

        builder.Skip().ThenSkip(policy => policy.ApplyTo<TimeoutException>());

        IErrorPolicy policy = builder.Build();
        ErrorPolicyChain chain = policy.ShouldBeOfType<ErrorPolicyChain>();
        chain.Policies.Count.ShouldBe(2);
        chain.Policies[0].ShouldBeOfType<SkipMessageErrorPolicy>();
        SkipMessageErrorPolicy skipMessageErrorPolicy = chain.Policies[1].ShouldBeOfType<SkipMessageErrorPolicy>();
        skipMessageErrorPolicy.IncludedExceptions.ShouldBe([typeof(TimeoutException)]);
    }

    [Fact]
    public void ThenRetry_ShouldAddDefaultRetryErrorPolicy()
    {
        ErrorPolicyBuilder builder = new();

        builder.Skip().ThenRetry();

        IErrorPolicy policy = builder.Build();
        ErrorPolicyChain chain = policy.ShouldBeOfType<ErrorPolicyChain>();
        chain.Policies.Count.ShouldBe(2);
        chain.Policies[0].ShouldBeOfType<SkipMessageErrorPolicy>();
        chain.Policies[1].ShouldBeEquivalentTo(new RetryErrorPolicy());
    }

    [Fact]
    public void ThenRetry_ShouldAddRetryErrorPolicyWithOptions()
    {
        ErrorPolicyBuilder builder = new();

        builder.Skip().ThenRetry(policy => policy.WithInterval(TimeSpan.FromDays(42)));
        IErrorPolicy policy = builder.Build();

        ErrorPolicyChain chain = policy.ShouldBeOfType<ErrorPolicyChain>();
        chain.Policies.Count.ShouldBe(2);
        chain.Policies[0].ShouldBeOfType<SkipMessageErrorPolicy>();
        RetryErrorPolicy retryPolicy = chain.Policies[1].ShouldBeOfType<RetryErrorPolicy>();
        retryPolicy.InitialDelay.ShouldBe(TimeSpan.FromDays(42));
    }

    [Fact]
    public void ThenRetry_ShouldAddRetryErrorPolicyWithRetriesCount()
    {
        ErrorPolicyBuilder builder = new();

        builder.Skip().ThenRetry(42);
        IErrorPolicy policy = builder.Build();

        ErrorPolicyChain chain = policy.ShouldBeOfType<ErrorPolicyChain>();
        chain.Policies.Count.ShouldBe(2);
        chain.Policies[0].ShouldBeOfType<SkipMessageErrorPolicy>();
        RetryErrorPolicy retryPolicy = chain.Policies[1].ShouldBeOfType<RetryErrorPolicy>();
        retryPolicy.MaxFailedAttempts.ShouldBe(42);
    }

    [Fact]
    public void ThenRetry_ShouldAddRetryErrorPolicyWithRetriesCountAndOptions()
    {
        ErrorPolicyBuilder builder = new();

        builder.Skip().ThenRetry(42, policy => policy.WithInterval(TimeSpan.FromDays(42)));
        IErrorPolicy policy = builder.Build();

        ErrorPolicyChain chain = policy.ShouldBeOfType<ErrorPolicyChain>();
        chain.Policies.Count.ShouldBe(2);
        chain.Policies[0].ShouldBeOfType<SkipMessageErrorPolicy>();
        RetryErrorPolicy retryPolicy = chain.Policies[1].ShouldBeOfType<RetryErrorPolicy>();
        retryPolicy.MaxFailedAttempts.ShouldBe(42);
        retryPolicy.InitialDelay.ShouldBe(TimeSpan.FromDays(42));
    }

    [Fact]
    public void ThenMoveTo_ShouldAddMoveMessageErrorPolicy()
    {
        ErrorPolicyBuilder builder = new();

        builder.Skip().ThenMoveTo("topic1");
        IErrorPolicy policy = builder.Build();

        ErrorPolicyChain chain = policy.ShouldBeOfType<ErrorPolicyChain>();
        chain.Policies.Count.ShouldBe(2);
        chain.Policies[0].ShouldBeOfType<SkipMessageErrorPolicy>();
        MoveMessageErrorPolicy movePolicy = chain.Policies[1].ShouldBeOfType<MoveMessageErrorPolicy>();
        movePolicy.EndpointName.ShouldBe("topic1");
    }

    [Fact]
    public void ThenMoveTo_ShouldAddMoveMessageErrorPolicyWithOptions()
    {
        ErrorPolicyBuilder builder = new();

        builder.Skip().ThenMoveTo("topic1", policy => policy.WithMaxRetries(42));
        IErrorPolicy policy = builder.Build();

        ErrorPolicyChain chain = policy.ShouldBeOfType<ErrorPolicyChain>();
        chain.Policies.Count.ShouldBe(2);
        chain.Policies[0].ShouldBeOfType<SkipMessageErrorPolicy>();
        MoveMessageErrorPolicy movePolicy = chain.Policies[1].ShouldBeOfType<MoveMessageErrorPolicy>();
        movePolicy.EndpointName.ShouldBe("topic1");
        movePolicy.MaxFailedAttempts.ShouldBe(42);
    }
}
