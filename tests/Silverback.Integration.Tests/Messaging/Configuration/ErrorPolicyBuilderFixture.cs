// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.ErrorHandling;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public class ErrorPolicyBuilderFixture
{
    [Fact]
    public void Stop_ShouldBuildDefaultStopErrorPolicy()
    {
        ErrorPolicyBuilder builder = new();

        builder.Stop();

        IErrorPolicy policy = builder.Build();
        policy.ShouldBeEquivalentTo(new StopConsumerErrorPolicy());
    }

    [Fact]
    public void Stop_ShouldBuildStopErrorPolicyWithOptions()
    {
        ErrorPolicyBuilder builder = new();

        builder.Stop(stopPolicy => stopPolicy.Exclude<InvalidOperationException>());

        IErrorPolicy policy = builder.Build();
        StopConsumerErrorPolicy stopPolicy = policy.ShouldBeOfType<StopConsumerErrorPolicy>();
        stopPolicy.ExcludedExceptions.ShouldBe([typeof(InvalidOperationException)]);
    }

    [Fact]
    public void Skip_ShouldBuildDefaultSkipMessageErrorPolicy()
    {
        ErrorPolicyBuilder builder = new();

        builder.Skip();

        IErrorPolicy policy = builder.Build();
        policy.ShouldBeEquivalentTo(new SkipMessageErrorPolicy());
    }

    [Fact]
    public void Skip_ShouldBuildSkipMessageErrorPolicyWithOptions()
    {
        ErrorPolicyBuilder builder = new();

        builder.Skip(policy => policy.ApplyTo<TimeoutException>());

        IErrorPolicy policy = builder.Build();
        SkipMessageErrorPolicy skipPolicy = policy.ShouldBeOfType<SkipMessageErrorPolicy>();
        skipPolicy.IncludedExceptions.ShouldBe([typeof(TimeoutException)]);
    }

    [Fact]
    public void Retry_ShouldBuildDefaultRetryErrorPolicy()
    {
        ErrorPolicyBuilder builder = new();

        builder.Retry();

        IErrorPolicy policy = builder.Build();
        policy.ShouldBeEquivalentTo(new RetryErrorPolicy());
    }

    [Fact]
    public void Retry_ShouldBuildRetryErrorPolicyWithOptions()
    {
        ErrorPolicyBuilder builder = new();

        builder.Retry(policy => policy.WithInterval(TimeSpan.FromDays(42)));
        IErrorPolicy policy = builder.Build();

        RetryErrorPolicy retryPolicy = policy.ShouldBeOfType<RetryErrorPolicy>();
        retryPolicy.InitialDelay.ShouldBe(TimeSpan.FromDays(42));
    }

    [Fact]
    public void Retry_ShouldBuildRetryErrorPolicyWithRetriesCount()
    {
        ErrorPolicyBuilder builder = new();

        builder.Retry(42);
        IErrorPolicy policy = builder.Build();

        RetryErrorPolicy retryPolicy = policy.ShouldBeOfType<RetryErrorPolicy>();
        retryPolicy.MaxFailedAttempts.ShouldBe(42);
    }

    [Fact]
    public void Retry_ShouldBuildRetryErrorPolicyWithRetriesCountAndOptions()
    {
        ErrorPolicyBuilder builder = new();

        builder.Retry(42, policy => policy.WithInterval(TimeSpan.FromDays(42)));
        IErrorPolicy policy = builder.Build();

        RetryErrorPolicy retryPolicy = policy.ShouldBeOfType<RetryErrorPolicy>();
        retryPolicy.MaxFailedAttempts.ShouldBe(42);
        retryPolicy.InitialDelay.ShouldBe(TimeSpan.FromDays(42));
    }

    [Fact]
    public void MoveTo_ShouldBuildMoveMessageErrorPolicy()
    {
        ErrorPolicyBuilder builder = new();

        builder.MoveTo("topic1");
        IErrorPolicy policy = builder.Build();

        MoveMessageErrorPolicy movePolicy = policy.ShouldBeOfType<MoveMessageErrorPolicy>();
        movePolicy.EndpointName.ShouldBe("topic1");
    }

    [Fact]
    public void MoveTo_ShouldBuildMoveMessageErrorPolicyWithOptions()
    {
        ErrorPolicyBuilder builder = new();

        builder.MoveTo("topic1", policy => policy.WithMaxRetries(42));
        IErrorPolicy policy = builder.Build();

        MoveMessageErrorPolicy movePolicy = policy.ShouldBeOfType<MoveMessageErrorPolicy>();
        movePolicy.EndpointName.ShouldBe("topic1");
        movePolicy.MaxFailedAttempts.ShouldBe(42);
    }
}
