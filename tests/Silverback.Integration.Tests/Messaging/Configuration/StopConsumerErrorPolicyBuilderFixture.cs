// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.ErrorHandling;
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
        policy.IncludedExceptions.ShouldBe([typeof(TimeoutException), typeof(OutOfMemoryException)]);
    }

    [Fact]
    public void ApplyTo_ShouldThrow_WhenTypeIsNull()
    {
        StopConsumerErrorPolicyBuilder builder = new();

        Action act = () => builder.ApplyTo(null!);

        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void ApplyTo_ShouldAddIncludedException_WhenSpecifiedViaGenericParameter()
    {
        StopConsumerErrorPolicyBuilder builder = new();

        builder.ApplyTo<TimeoutException>().ApplyTo<OutOfMemoryException>();

        StopConsumerErrorPolicy policy = (StopConsumerErrorPolicy)builder.Build();
        policy.IncludedExceptions.ShouldBe([typeof(TimeoutException), typeof(OutOfMemoryException)]);
    }

    [Fact]
    public void Exclude_ShouldAddExcludedExceptions()
    {
        StopConsumerErrorPolicyBuilder builder = new();

        builder.Exclude(typeof(TimeoutException)).Exclude(typeof(OutOfMemoryException));

        StopConsumerErrorPolicy policy = (StopConsumerErrorPolicy)builder.Build();
        policy.ExcludedExceptions.ShouldBe([typeof(TimeoutException), typeof(OutOfMemoryException)]);
    }

    [Fact]
    public void Exclude_ShouldThrow_WhenTypeIsNull()
    {
        StopConsumerErrorPolicyBuilder builder = new();

        Action act = () => builder.Exclude(null!);

        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void Exclude_ShouldAddExcludedException_WhenSpecifiedViaGenericParameter()
    {
        StopConsumerErrorPolicyBuilder builder = new();

        builder.Exclude<TimeoutException>().Exclude<OutOfMemoryException>();

        StopConsumerErrorPolicy policy = (StopConsumerErrorPolicy)builder.Build();
        policy.ExcludedExceptions.ShouldBe([typeof(TimeoutException), typeof(OutOfMemoryException)]);
    }

    [Fact]
    public void ApplyWhen_ShouldSetApplyRule()
    {
        StopConsumerErrorPolicyBuilder builder = new();

        builder.ApplyWhen(_ => true);

        StopConsumerErrorPolicy policy = (StopConsumerErrorPolicy)builder.Build();
        policy.ApplyRule.ShouldNotBeNull();
        policy.ApplyRule.Invoke(null!, null!).ShouldBeTrue();
    }

    [Fact]
    public void ApplyWhen_ShouldThrow_WhenFunctionIsNull()
    {
        StopConsumerErrorPolicyBuilder builder = new();

        Action act1 = () => builder.ApplyWhen((Func<IRawInboundEnvelope, bool>)null!);
        Action act2 = () => builder.ApplyWhen((Func<IRawInboundEnvelope, Exception, bool>)null!);

        act1.ShouldThrow<ArgumentNullException>();
        act2.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void ApplyWhen_ShouldSetApplyRuleWithExceptionParameter()
    {
        StopConsumerErrorPolicyBuilder builder = new();

        builder.ApplyWhen((_, _) => true);

        StopConsumerErrorPolicy policy = (StopConsumerErrorPolicy)builder.Build();
        policy.ApplyRule.ShouldNotBeNull();
        policy.ApplyRule.Invoke(null!, null!).ShouldBeTrue();
    }

    [Fact]
    public void Publish_ShouldSetMessageFactory()
    {
        StopConsumerErrorPolicyBuilder builder = new();

        builder.Publish(_ => new TestEventOne());

        StopConsumerErrorPolicy policy = (StopConsumerErrorPolicy)builder.Build();
        policy.MessageToPublishFactory.ShouldNotBeNull();
        policy.MessageToPublishFactory.Invoke(null!, null!).ShouldBeOfType<TestEventOne>();
    }

    [Fact]
    public void Publish_ShouldThrow_WhenFunctionIsNull()
    {
        StopConsumerErrorPolicyBuilder builder = new();

        Action act1 = () => builder.Publish((Func<IRawInboundEnvelope, object?>)null!);
        Action act2 = () => builder.Publish((Func<IRawInboundEnvelope, Exception, object?>)null!);

        act1.ShouldThrow<ArgumentNullException>();
        act2.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void Publish_ShouldSetMessageFactoryWithExceptionParameter()
    {
        StopConsumerErrorPolicyBuilder builder = new();

        builder.Publish((_, _) => new TestEventOne());

        StopConsumerErrorPolicy policy = (StopConsumerErrorPolicy)builder.Build();
        policy.MessageToPublishFactory.ShouldNotBeNull();
        policy.MessageToPublishFactory.Invoke(null!, null!).ShouldBeOfType<TestEventOne>();
    }
}
