// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
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
        policy.Should().BeOfType<StopConsumerErrorPolicy>();
        policy.Should().BeEquivalentTo(new StopConsumerErrorPolicy());
    }

    [Fact]
    public void Stop_ShouldBuildStopErrorPolicyWithOptions()
    {
        ErrorPolicyBuilder builder = new();

        builder.Stop(stopPolicy => stopPolicy.Exclude<InvalidOperationException>());

        IErrorPolicy policy = builder.Build();
        policy.Should().BeOfType<StopConsumerErrorPolicy>();
        policy.As<StopConsumerErrorPolicy>().ExcludedExceptions.Should().BeEquivalentTo(new[] { typeof(InvalidOperationException) });
    }

    [Fact]
    public void Skip_ShouldBuildDefaultSkipMessageErrorPolicy()
    {
        ErrorPolicyBuilder builder = new();

        builder.Skip();

        IErrorPolicy policy = builder.Build();
        policy.Should().BeOfType<SkipMessageErrorPolicy>();
        policy.Should().BeEquivalentTo(new SkipMessageErrorPolicy());
    }

    [Fact]
    public void Skip_ShouldBuildSkipMessageErrorPolicyWithOptions()
    {
        ErrorPolicyBuilder builder = new();

        builder.Skip(policy => policy.ApplyTo<TimeoutException>());

        IErrorPolicy policy = builder.Build();
        policy.Should().BeOfType<SkipMessageErrorPolicy>();
        policy.As<SkipMessageErrorPolicy>().IncludedExceptions.Should().BeEquivalentTo(new[] { typeof(TimeoutException) });
    }

    [Fact]
    public void Retry_ShouldBuildDefaultRetryErrorPolicy()
    {
        ErrorPolicyBuilder builder = new();

        builder.Retry();
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<RetryErrorPolicy>();
        policy.Should().BeEquivalentTo(new RetryErrorPolicy());
    }

    [Fact]
    public void Retry_ShouldBuildRetryErrorPolicyWithOptions()
    {
        ErrorPolicyBuilder builder = new();

        builder.Retry(policy => policy.WithInterval(TimeSpan.FromDays(42)));
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<RetryErrorPolicy>();
        policy.As<RetryErrorPolicy>().InitialDelay.Should().Be(TimeSpan.FromDays(42));
    }

    [Fact]
    public void Retry_ShouldBuildRetryErrorPolicyWithRetriesCount()
    {
        ErrorPolicyBuilder builder = new();

        builder.Retry(42);
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<RetryErrorPolicy>();
        policy.As<RetryErrorPolicy>().MaxFailedAttempts.Should().Be(42);
    }

    [Fact]
    public void Retry_ShouldBuildRetryErrorPolicyWithRetriesCountAndOptions()
    {
        ErrorPolicyBuilder builder = new();

        builder.Retry(42, policy => policy.WithInterval(TimeSpan.FromDays(42)));
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<RetryErrorPolicy>();
        policy.As<RetryErrorPolicy>().MaxFailedAttempts.Should().Be(42);
        policy.As<RetryErrorPolicy>().InitialDelay.Should().Be(TimeSpan.FromDays(42));
    }

    [Fact]
    public void MoveTo_ShouldBuildMoveMessageErrorPolicy()
    {
        ErrorPolicyBuilder builder = new();

        builder.MoveTo("topic1");
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<MoveMessageErrorPolicy>();
        policy.As<MoveMessageErrorPolicy>().EndpointName.Should().Be("topic1");
    }

    [Fact]
    public void MoveTo_ShouldBuildMoveMessageErrorPolicyWithOptions()
    {
        ErrorPolicyBuilder builder = new();

        builder.MoveTo("topic1", policy => policy.WithMaxRetries(42));
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<MoveMessageErrorPolicy>();
        policy.As<MoveMessageErrorPolicy>().EndpointName.Should().Be("topic1");
        policy.As<MoveMessageErrorPolicy>().MaxFailedAttempts.Should().Be(42);
    }
}
