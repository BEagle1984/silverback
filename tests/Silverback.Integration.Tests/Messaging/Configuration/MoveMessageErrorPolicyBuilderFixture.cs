// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public class MoveMessageErrorPolicyBuilderFixture
{
    [Fact]
    public void ApplyTo_ShouldAddIncludedExceptions()
    {
        MoveMessageErrorPolicyBuilder builder = new("topic1");

        builder.ApplyTo(typeof(TimeoutException)).ApplyTo(typeof(OutOfMemoryException));

        MoveMessageErrorPolicy policy = (MoveMessageErrorPolicy)builder.Build();
        policy.IncludedExceptions.Should().BeEquivalentTo(new[] { typeof(TimeoutException), typeof(OutOfMemoryException) });
    }

    [Fact]
    public void ApplyTo_ShouldThrow_WhenTypeIsNull()
    {
        MoveMessageErrorPolicyBuilder builder = new("topic1");

        Action act = () => builder.ApplyTo(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ApplyTo_ShouldAddIncludedException_WhenSpecifiedViaGenericParameter()
    {
        MoveMessageErrorPolicyBuilder builder = new("topic1");

        builder.ApplyTo<TimeoutException>().ApplyTo<OutOfMemoryException>();

        MoveMessageErrorPolicy policy = (MoveMessageErrorPolicy)builder.Build();
        policy.IncludedExceptions.Should().BeEquivalentTo(new[] { typeof(TimeoutException), typeof(OutOfMemoryException) });
    }

    [Fact]
    public void Exclude_ShouldAddExcludedExceptions()
    {
        MoveMessageErrorPolicyBuilder builder = new("topic1");

        builder.Exclude(typeof(TimeoutException)).Exclude(typeof(OutOfMemoryException));

        MoveMessageErrorPolicy policy = (MoveMessageErrorPolicy)builder.Build();
        policy.ExcludedExceptions.Should().BeEquivalentTo(new[] { typeof(TimeoutException), typeof(OutOfMemoryException) });
    }

    [Fact]
    public void Exclude_ShouldThrow_WhenTypeIsNull()
    {
        MoveMessageErrorPolicyBuilder builder = new("topic1");

        Action act = () => builder.Exclude(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Exclude_ShouldAddExcludedException_WhenSpecifiedViaGenericParameter()
    {
        MoveMessageErrorPolicyBuilder builder = new("topic1");

        builder.Exclude<TimeoutException>().Exclude<OutOfMemoryException>();

        MoveMessageErrorPolicy policy = (MoveMessageErrorPolicy)builder.Build();
        policy.ExcludedExceptions.Should().BeEquivalentTo(new[] { typeof(TimeoutException), typeof(OutOfMemoryException) });
    }

    [Fact]
    public void ApplyWhen_ShouldSetApplyRule()
    {
        MoveMessageErrorPolicyBuilder builder = new("topic1");

        builder.ApplyWhen(_ => true);

        MoveMessageErrorPolicy policy = (MoveMessageErrorPolicy)builder.Build();
        policy.ApplyRule.ShouldNotBeNull();
        policy.ApplyRule.Invoke(null!, null!).Should().BeTrue();
    }

    [Fact]
    public void ApplyWhen_ShouldThrow_WhenFunctionIsNull()
    {
        MoveMessageErrorPolicyBuilder builder = new("topic1");

        Action act1 = () => builder.ApplyWhen((Func<IRawInboundEnvelope, bool>)null!);
        Action act2 = () => builder.ApplyWhen((Func<IRawInboundEnvelope, Exception, bool>)null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ApplyWhen_ShouldSetApplyRuleWithExceptionParameter()
    {
        MoveMessageErrorPolicyBuilder builder = new("topic1");

        builder.ApplyWhen((_, _) => true);

        MoveMessageErrorPolicy policy = (MoveMessageErrorPolicy)builder.Build();
        policy.ApplyRule.ShouldNotBeNull();
        policy.ApplyRule.Invoke(null!, null!).Should().BeTrue();
    }

    [Fact]
    public void Publish_ShouldSetMessageFactory()
    {
        MoveMessageErrorPolicyBuilder builder = new("topic1");

        builder.Publish(_ => new TestEventOne());

        MoveMessageErrorPolicy policy = (MoveMessageErrorPolicy)builder.Build();
        policy.MessageToPublishFactory.ShouldNotBeNull();
        policy.MessageToPublishFactory.Invoke(null!, null!).Should().BeOfType<TestEventOne>();
    }

    [Fact]
    public void Publish_ShouldThrow_WhenFunctionIsNull()
    {
        MoveMessageErrorPolicyBuilder builder = new("topic1");

        Action act1 = () => builder.Publish((Func<IRawInboundEnvelope, object?>)null!);
        Action act2 = () => builder.Publish((Func<IRawInboundEnvelope, Exception, object?>)null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Publish_ShouldSetMessageFactoryWithExceptionParameter()
    {
        MoveMessageErrorPolicyBuilder builder = new("topic1");

        builder.Publish((_, _) => new TestEventOne());

        MoveMessageErrorPolicy policy = (MoveMessageErrorPolicy)builder.Build();
        policy.MessageToPublishFactory.ShouldNotBeNull();
        policy.MessageToPublishFactory.Invoke(null!, null!).Should().BeOfType<TestEventOne>();
    }

    [Fact]
    public void WithMaxRetries_ShouldSetMaxFailedAttempts()
    {
        MoveMessageErrorPolicyBuilder builder = new("topic1");

        builder.WithMaxRetries(42);

        MoveMessageErrorPolicy policy = (MoveMessageErrorPolicy)builder.Build();
        policy.MaxFailedAttempts.Should().Be(42);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-42)]
    public void WithMaxRetries_ShouldThrow_WhenRetriesIsLowerThanOne(int value)
    {
        MoveMessageErrorPolicyBuilder builder = new("topic1");

        Action act = () => builder.WithMaxRetries(value);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Transform_ShouldSetTransformAction()
    {
        MoveMessageErrorPolicyBuilder builder = new("topic1");

        builder.Transform(
            _ =>
            {
            });

        MoveMessageErrorPolicy policy = (MoveMessageErrorPolicy)builder.Build();
        policy.TransformMessageAction.ShouldNotBeNull();
    }

    [Fact]
    public void Transform_ShouldThrow_WhenFunctionIsNull()
    {
        MoveMessageErrorPolicyBuilder builder = new("topic1");

        Action act1 = () => builder.Transform((Action<IOutboundEnvelope?>)null!);
        Action act2 = () => builder.Publish((Func<IRawInboundEnvelope, Exception, object?>)null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Transform_ShouldSetMessageFactoryWithExceptionParameter()
    {
        MoveMessageErrorPolicyBuilder builder = new("topic1");

        builder.Transform(
            (_, _) =>
            {
            });

        MoveMessageErrorPolicy policy = (MoveMessageErrorPolicy)builder.Build();
        policy.TransformMessageAction.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_ShouldSetEndpointName()
    {
        MoveMessageErrorPolicyBuilder builder = new("topic1");

        MoveMessageErrorPolicy policy = (MoveMessageErrorPolicy)builder.Build();
        policy.EndpointName.Should().Be("topic1");
    }
}
