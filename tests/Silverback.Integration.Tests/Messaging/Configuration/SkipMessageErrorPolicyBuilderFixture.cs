// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public class SkipMessageErrorPolicyBuilderFixture
{
    [Fact]
    public void ApplyTo_ShouldAddIncludedExceptions()
    {
        SkipMessageErrorPolicyBuilder builder = new();

        builder.ApplyTo(typeof(TimeoutException)).ApplyTo(typeof(OutOfMemoryException));

        SkipMessageErrorPolicy policy = (SkipMessageErrorPolicy)builder.Build();
        policy.IncludedExceptions.Should().BeEquivalentTo(new[] { typeof(TimeoutException), typeof(OutOfMemoryException) });
    }

    [Fact]
    public void ApplyTo_ShouldThrow_WhenTypeIsNull()
    {
        SkipMessageErrorPolicyBuilder builder = new();

        Action act = () => builder.ApplyTo(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ApplyTo_ShouldAddIncludedException_WhenSpecifiedViaGenericParameter()
    {
        SkipMessageErrorPolicyBuilder builder = new();

        builder.ApplyTo<TimeoutException>().ApplyTo<OutOfMemoryException>();

        SkipMessageErrorPolicy policy = (SkipMessageErrorPolicy)builder.Build();
        policy.IncludedExceptions.Should().BeEquivalentTo(new[] { typeof(TimeoutException), typeof(OutOfMemoryException) });
    }

    [Fact]
    public void Exclude_ShouldAddExcludedExceptions()
    {
        SkipMessageErrorPolicyBuilder builder = new();

        builder.Exclude(typeof(TimeoutException)).Exclude(typeof(OutOfMemoryException));

        SkipMessageErrorPolicy policy = (SkipMessageErrorPolicy)builder.Build();
        policy.ExcludedExceptions.Should().BeEquivalentTo(new[] { typeof(TimeoutException), typeof(OutOfMemoryException) });
    }

    [Fact]
    public void Exclude_ShouldThrow_WhenTypeIsNull()
    {
        SkipMessageErrorPolicyBuilder builder = new();

        Action act = () => builder.Exclude(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Exclude_ShouldAddExcludedException_WhenSpecifiedViaGenericParameter()
    {
        SkipMessageErrorPolicyBuilder builder = new();

        builder.Exclude<TimeoutException>().Exclude<OutOfMemoryException>();

        SkipMessageErrorPolicy policy = (SkipMessageErrorPolicy)builder.Build();
        policy.ExcludedExceptions.Should().BeEquivalentTo(new[] { typeof(TimeoutException), typeof(OutOfMemoryException) });
    }

    [Fact]
    public void ApplyWhen_ShouldSetApplyRule()
    {
        SkipMessageErrorPolicyBuilder builder = new();

        builder.ApplyWhen(_ => true);

        SkipMessageErrorPolicy policy = (SkipMessageErrorPolicy)builder.Build();
        policy.ApplyRule.ShouldNotBeNull();
        policy.ApplyRule.Invoke(null!, null!).Should().BeTrue();
    }

    [Fact]
    public void ApplyWhen_ShouldThrow_WhenFunctionIsNull()
    {
        SkipMessageErrorPolicyBuilder builder = new();

        Action act1 = () => builder.ApplyWhen((Func<IRawInboundEnvelope, bool>)null!);
        Action act2 = () => builder.ApplyWhen((Func<IRawInboundEnvelope, Exception, bool>)null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ApplyWhen_ShouldSetApplyRuleWithExceptionParameter()
    {
        SkipMessageErrorPolicyBuilder builder = new();

        builder.ApplyWhen((_, _) => true);

        SkipMessageErrorPolicy policy = (SkipMessageErrorPolicy)builder.Build();
        policy.ApplyRule.ShouldNotBeNull();
        policy.ApplyRule.Invoke(null!, null!).Should().BeTrue();
    }

    [Fact]
    public void Publish_ShouldSetMessageFactory()
    {
        SkipMessageErrorPolicyBuilder builder = new();

        builder.Publish(_ => new TestEventOne());

        SkipMessageErrorPolicy policy = (SkipMessageErrorPolicy)builder.Build();
        policy.MessageToPublishFactory.ShouldNotBeNull();
        policy.MessageToPublishFactory.Invoke(null!, null!).Should().BeOfType<TestEventOne>();
    }

    [Fact]
    public void Publish_ShouldThrow_WhenFunctionIsNull()
    {
        SkipMessageErrorPolicyBuilder builder = new();

        Action act1 = () => builder.Publish((Func<IRawInboundEnvelope, object?>)null!);
        Action act2 = () => builder.Publish((Func<IRawInboundEnvelope, Exception, object?>)null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Publish_ShouldSetMessageFactoryWithExceptionParameter()
    {
        SkipMessageErrorPolicyBuilder builder = new();

        builder.Publish((_, _) => new TestEventOne());

        SkipMessageErrorPolicy policy = (SkipMessageErrorPolicy)builder.Build();
        policy.MessageToPublishFactory.ShouldNotBeNull();
        policy.MessageToPublishFactory.Invoke(null!, null!).Should().BeOfType<TestEventOne>();
    }
}
