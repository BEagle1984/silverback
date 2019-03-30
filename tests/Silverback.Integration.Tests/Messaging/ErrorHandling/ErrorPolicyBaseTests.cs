// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling
{
    public class ErrorPolicyBaseTests
    {
        [Theory, ClassData(typeof(ApplyTo_TestData))]
        public void ApplyTo_Exception_CanHandleReturnsExpectedResult(Exception exception, bool mustApply)
        {
            var policy = new TestErrorPolicy()
                .ApplyTo<ArgumentException>()
                .ApplyTo<InvalidCastException>();

            var canHandle = policy.CanHandle(new FailedMessage(new TestEventOne(), 99), exception);

            canHandle.Should().Be(mustApply);
        }

        [Theory, ClassData(typeof(Exclude_TestData))]
        public void Exclude_Exception_CanHandleReturnsExpectedResult(Exception exception, bool mustApply)
        {
            var policy = (TestErrorPolicy)new TestErrorPolicy()
                .Exclude<ArgumentException>()
                .Exclude<InvalidCastException>();

            var canHandle = policy.CanHandle(new FailedMessage(new TestEventOne(), 99), exception);

            canHandle.Should().Be(mustApply);
        }

        [Theory, ClassData(typeof(ApplyToAndExclude_TestData))]
        public void ApplyToAndExcludeTest(Exception exception, bool mustApply)
        {
            var policy = (TestErrorPolicy)new TestErrorPolicy()
                .ApplyTo<ArgumentException>()
                .Exclude<ArgumentOutOfRangeException>()
                .ApplyTo<FormatException>();

            var canHandle = policy.CanHandle(new FailedMessage(new TestEventOne(), 99), exception);

            canHandle.Should().Be(mustApply);
        }

        [Theory, ClassData(typeof(ApplyWhen_TestData))]
        public void ApplyWhenTest(FailedMessage message, Exception exception, bool mustApply)
        {
            var policy = (TestErrorPolicy)new TestErrorPolicy()
                .ApplyWhen((msg, ex) => msg.FailedAttempts <= 5 && ex.Message != "no");

            var canHandle = policy.CanHandle(message, exception);

            canHandle.Should().Be(mustApply);
        }
    }
}