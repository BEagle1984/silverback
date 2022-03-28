// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public class RetryErrorPolicyBuilderFixture
{
    [Fact]
    public void ApplyTo_ShouldAddIncludedExceptions()
    {
        RetryErrorPolicyBuilder builder = new();

        builder.ApplyTo(typeof(TimeoutException)).ApplyTo(typeof(OutOfMemoryException));

        RetryErrorPolicy policy = (RetryErrorPolicy)builder.Build();
        policy.IncludedExceptions.Should().BeEquivalentTo(new[] { typeof(TimeoutException), typeof(OutOfMemoryException) });
    }

    [Fact]
    public void ApplyTo_ShouldThrow_WhenTypeIsNull()
    {
        RetryErrorPolicyBuilder builder = new();

        Action act = () => builder.ApplyTo(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ApplyTo_ShouldAddIncludedException_WhenSpecifiedViaGenericParameter()
    {
        RetryErrorPolicyBuilder builder = new();

        builder.ApplyTo<TimeoutException>().ApplyTo<OutOfMemoryException>();

        RetryErrorPolicy policy = (RetryErrorPolicy)builder.Build();
        policy.IncludedExceptions.Should().BeEquivalentTo(new[] { typeof(TimeoutException), typeof(OutOfMemoryException) });
    }

    [Fact]
    public void Exclude_ShouldAddExcludedExceptions()
    {
        RetryErrorPolicyBuilder builder = new();

        builder.Exclude(typeof(TimeoutException)).Exclude(typeof(OutOfMemoryException));

        RetryErrorPolicy policy = (RetryErrorPolicy)builder.Build();
        policy.ExcludedExceptions.Should().BeEquivalentTo(new[] { typeof(TimeoutException), typeof(OutOfMemoryException) });
    }

    [Fact]
    public void Exclude_ShouldThrow_WhenTypeIsNull()
    {
        RetryErrorPolicyBuilder builder = new();

        Action act = () => builder.Exclude(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Exclude_ShouldAddExcludedException_WhenSpecifiedViaGenericParameter()
    {
        RetryErrorPolicyBuilder builder = new();

        builder.Exclude<TimeoutException>().Exclude<OutOfMemoryException>();

        RetryErrorPolicy policy = (RetryErrorPolicy)builder.Build();
        policy.ExcludedExceptions.Should().BeEquivalentTo(new[] { typeof(TimeoutException), typeof(OutOfMemoryException) });
    }

    [Fact]
    public void ApplyWhen_ShouldSetApplyRule()
    {
        RetryErrorPolicyBuilder builder = new();

        builder.ApplyWhen(_ => true);

        RetryErrorPolicy policy = (RetryErrorPolicy)builder.Build();
        policy.ApplyRule.ShouldNotBeNull();
        policy.ApplyRule.Invoke(null!, null!).Should().BeTrue();
    }

    [Fact]
    public void ApplyWhen_ShouldThrow_WhenFunctionIsNull()
    {
        RetryErrorPolicyBuilder builder = new();

        Action act1 = () => builder.ApplyWhen((Func<IRawInboundEnvelope, bool>)null!);
        Action act2 = () => builder.ApplyWhen((Func<IRawInboundEnvelope, Exception, bool>)null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ApplyWhen_ShouldSetApplyRuleWithExceptionParameter()
    {
        RetryErrorPolicyBuilder builder = new();

        builder.ApplyWhen((_, _) => true);

        RetryErrorPolicy policy = (RetryErrorPolicy)builder.Build();
        policy.ApplyRule.ShouldNotBeNull();
        policy.ApplyRule.Invoke(null!, null!).Should().BeTrue();
    }

    [Fact]
    public void Publish_ShouldSetMessageFactory()
    {
        RetryErrorPolicyBuilder builder = new();

        builder.Publish(_ => new TestEventOne());

        RetryErrorPolicy policy = (RetryErrorPolicy)builder.Build();
        policy.MessageToPublishFactory.ShouldNotBeNull();
        policy.MessageToPublishFactory.Invoke(null!, null!).Should().BeOfType<TestEventOne>();
    }

    [Fact]
    public void Publish_ShouldThrow_WhenFunctionIsNull()
    {
        RetryErrorPolicyBuilder builder = new();

        Action act1 = () => builder.Publish((Func<IRawInboundEnvelope, object?>)null!);
        Action act2 = () => builder.Publish((Func<IRawInboundEnvelope, Exception, object?>)null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Publish_ShouldSetMessageFactoryWithExceptionParameter()
    {
        RetryErrorPolicyBuilder builder = new();

        builder.Publish((_, _) => new TestEventOne());

        RetryErrorPolicy policy = (RetryErrorPolicy)builder.Build();
        policy.MessageToPublishFactory.ShouldNotBeNull();
        policy.MessageToPublishFactory.Invoke(null!, null!).Should().BeOfType<TestEventOne>();
    }

    [Fact]
    public void WithInterval_ShouldSetInitialDelay()
    {
        RetryErrorPolicyBuilder builder = new();

        builder.WithInterval(TimeSpan.FromMinutes(42));

        RetryErrorPolicy policy = (RetryErrorPolicy)builder.Build();
        policy.InitialDelay.Should().Be(TimeSpan.FromMinutes(42));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-42)]
    public void WithInterval_ShouldThrow_WhenIntervalIsLowerOrEqualToZero(int value)
    {
        RetryErrorPolicyBuilder builder = new();

        Action act = () => builder.WithInterval(TimeSpan.FromMinutes(value));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WithIncrementalDelay_ShouldSetInitialDelayAndIncrement()
    {
        RetryErrorPolicyBuilder builder = new();

        builder.WithIncrementalDelay(TimeSpan.FromMinutes(42), TimeSpan.FromDays(42));

        RetryErrorPolicy policy = (RetryErrorPolicy)builder.Build();
        policy.InitialDelay.Should().Be(TimeSpan.FromMinutes(42));
        policy.DelayIncrement.Should().Be(TimeSpan.FromDays(42));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-42)]
    public void WithInterval_ShouldThrow_WhenInitialDelayIsLowerOrEqualToZero(int value)
    {
        RetryErrorPolicyBuilder builder = new();

        Action act = () => builder.WithIncrementalDelay(TimeSpan.FromMinutes(value), TimeSpan.MaxValue);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-42)]
    public void WithInterval_ShouldThrow_WhenDelayIncrementIsLowerOrEqualToZero(int value)
    {
        RetryErrorPolicyBuilder builder = new();

        Action act = () => builder.WithIncrementalDelay(TimeSpan.MaxValue, TimeSpan.FromMinutes(value));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WithMaxRetries_ShouldSetMaxFailedAttempts()
    {
        RetryErrorPolicyBuilder builder = new();

        builder.WithMaxRetries(42);

        RetryErrorPolicy policy = (RetryErrorPolicy)builder.Build();
        policy.MaxFailedAttempts.Should().Be(42);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-42)]
    public void WithMaxRetries_ShouldThrow_WhenRetriesIsLowerThanOne(int value)
    {
        RetryErrorPolicyBuilder builder = new();

        Action act = () => builder.WithMaxRetries(value);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
