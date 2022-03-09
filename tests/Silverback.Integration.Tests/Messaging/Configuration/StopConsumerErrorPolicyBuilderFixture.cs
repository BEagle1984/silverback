// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public class StopConsumerErrorPolicyBuilderFixture
{
    [Fact]
    public void ApplyTo_ShouldAddIncludedExceptions()
    {
        StopConsumerErrorPolicyBuilder builder = new();

        builder.ApplyTo(typeof(TimeoutException)).ApplyTo(typeof(OutOfMemoryException));

        StopConsumerErrorPolicy policy = (StopConsumerErrorPolicy)builder.Build();
        policy.IncludedExceptions.Should().BeEquivalentTo(new[] { typeof(TimeoutException), typeof(OutOfMemoryException) });
    }

    [Fact]
    public void ApplyTo_ShouldThrow_WhenTypeIsNull()
    {
        StopConsumerErrorPolicyBuilder builder = new();

        Action act = () => builder.ApplyTo(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ApplyTo_ShouldAddIncludedException_WhenSpecifiedViaGenericParameter()
    {
        StopConsumerErrorPolicyBuilder builder = new();

        builder.ApplyTo<TimeoutException>().ApplyTo<OutOfMemoryException>();

        StopConsumerErrorPolicy policy = (StopConsumerErrorPolicy)builder.Build();
        policy.IncludedExceptions.Should().BeEquivalentTo(new[] { typeof(TimeoutException), typeof(OutOfMemoryException) });
    }

    [Fact]
    public void Exclude_ShouldAddExcludedExceptions()
    {
        StopConsumerErrorPolicyBuilder builder = new();

        builder.Exclude(typeof(TimeoutException)).Exclude(typeof(OutOfMemoryException));

        StopConsumerErrorPolicy policy = (StopConsumerErrorPolicy)builder.Build();
        policy.ExcludedExceptions.Should().BeEquivalentTo(new[] { typeof(TimeoutException), typeof(OutOfMemoryException) });
    }

    [Fact]
    public void Exclude_ShouldThrow_WhenTypeIsNull()
    {
        StopConsumerErrorPolicyBuilder builder = new();

        Action act = () => builder.Exclude(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Exclude_ShouldAddExcludedException_WhenSpecifiedViaGenericParameter()
    {
        StopConsumerErrorPolicyBuilder builder = new();

        builder.Exclude<TimeoutException>().Exclude<OutOfMemoryException>();

        StopConsumerErrorPolicy policy = (StopConsumerErrorPolicy)builder.Build();
        policy.ExcludedExceptions.Should().BeEquivalentTo(new[] { typeof(TimeoutException), typeof(OutOfMemoryException) });
    }

    [Fact]
    public void ApplyWhen_ShouldSetApplyRule()
    {
        StopConsumerErrorPolicyBuilder builder = new();

        builder.ApplyWhen(_ => true);

        StopConsumerErrorPolicy policy = (StopConsumerErrorPolicy)builder.Build();
        policy.ApplyRule.ShouldNotBeNull();
        policy.ApplyRule.Invoke(null!, null!).Should().BeTrue();
    }

    [Fact]
    public void ApplyWhen_ShouldThrow_WhenFunctionIsNull()
    {
        StopConsumerErrorPolicyBuilder builder = new();

        Action act1 = () => builder.ApplyWhen((Func<IRawInboundEnvelope, bool>)null!);
        Action act2 = () => builder.ApplyWhen((Func<IRawInboundEnvelope, Exception, bool>)null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ApplyWhen_ShouldSetApplyRuleWithExceptionParameter()
    {
        StopConsumerErrorPolicyBuilder builder = new();

        builder.ApplyWhen((_, _) => true);

        StopConsumerErrorPolicy policy = (StopConsumerErrorPolicy)builder.Build();
        policy.ApplyRule.ShouldNotBeNull();
        policy.ApplyRule.Invoke(null!, null!).Should().BeTrue();
    }

    [Fact]
    public void Publish_ShouldSetMessageFactory()
    {
        StopConsumerErrorPolicyBuilder builder = new();

        builder.Publish(_ => new TestEventOne());

        StopConsumerErrorPolicy policy = (StopConsumerErrorPolicy)builder.Build();
        policy.MessageToPublishFactory.ShouldNotBeNull();
        policy.MessageToPublishFactory.Invoke(null!, null!).Should().BeOfType<TestEventOne>();
    }

    [Fact]
    public void Publish_ShouldThrow_WhenFunctionIsNull()
    {
        StopConsumerErrorPolicyBuilder builder = new();

        Action act1 = () => builder.Publish((Func<IRawInboundEnvelope, object?>)null!);
        Action act2 = () => builder.Publish((Func<IRawInboundEnvelope, Exception, object?>)null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Publish_ShouldSetMessageFactoryWithExceptionParameter()
    {
        StopConsumerErrorPolicyBuilder builder = new();

        builder.Publish((_, _) => new TestEventOne());

        StopConsumerErrorPolicy policy = (StopConsumerErrorPolicy)builder.Build();
        policy.MessageToPublishFactory.ShouldNotBeNull();
        policy.MessageToPublishFactory.Invoke(null!, null!).Should().BeOfType<TestEventOne>();
    }
}
