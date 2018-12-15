// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.ErrorHandling
{
    [TestFixture]
    public class ErrorPolicyBaseTests
    {
        public static IEnumerable<TestCaseData> ApplyToTestData
        {
            get
            {
                yield return new TestCaseData(new ArgumentException(), true);
                yield return new TestCaseData(new ArgumentOutOfRangeException(), true);
                yield return new TestCaseData(new InvalidCastException(), true);
                yield return new TestCaseData(new FormatException(), false);
            }
        }

        [Test]
        [TestCaseSource(nameof(ApplyToTestData))]
        public void ApplyToTest(Exception exception, bool mustApply)
        {
            var policy = new TestErrorPolicy()
                .ApplyTo<ArgumentException>()
                .ApplyTo<InvalidCastException>();

            var canHandle = policy.CanHandle(new FailedMessage(new TestEventOne(), 99), exception);

            Assert.That(canHandle, Is.EqualTo(mustApply));
        }

        public static IEnumerable<TestCaseData> ExcludeTestData
        {
            get
            {
                yield return new TestCaseData(new ArgumentException(), false);
                yield return new TestCaseData(new ArgumentOutOfRangeException(), false);
                yield return new TestCaseData(new InvalidCastException(), false);
                yield return new TestCaseData(new FormatException(), true);
            }
        }

        [Test]
        [TestCaseSource(nameof(ExcludeTestData))]
        public void ExcludeTest(Exception exception, bool mustApply)
        {
            var policy = (TestErrorPolicy)new TestErrorPolicy()
                .Exclude<ArgumentException>()
                .Exclude<InvalidCastException>();

            var canHandle = policy.CanHandle(new FailedMessage(new TestEventOne(), 99), exception);

            Assert.That(canHandle, Is.EqualTo(mustApply));
        }

        public static IEnumerable<TestCaseData> ApplyToAndExcludeTestTestData
        {
            get
            {
                yield return new TestCaseData(new ArgumentException(), true);
                yield return new TestCaseData(new ArgumentNullException(), true);
                yield return new TestCaseData(new ArgumentOutOfRangeException(), false);
                yield return new TestCaseData(new InvalidCastException(), false);
                yield return new TestCaseData(new FormatException(), true);
            }
        }

        [Test]
        [TestCaseSource(nameof(ApplyToAndExcludeTestTestData))]
        public void ApplyToAndExcludeTest(Exception exception, bool mustApply)
        {
            var policy = (TestErrorPolicy)new TestErrorPolicy()
                .ApplyTo<ArgumentException>()
                .Exclude<ArgumentOutOfRangeException>()
                .ApplyTo<FormatException>();

            var canHandle = policy.CanHandle(new FailedMessage(new TestEventOne(), 99), exception);

            Assert.That(canHandle, Is.EqualTo(mustApply));
        }

        public static IEnumerable<TestCaseData> ApplyWhenTestData
        {
            get
            {
                yield return new TestCaseData(
                    new FailedMessage(new TestEventOne(), 3),
                    new ArgumentException(),
                    true);
                yield return new TestCaseData(
                    new FailedMessage(new TestEventOne(), 6),
                    new ArgumentException(),
                    false);
                yield return new TestCaseData(
                    new FailedMessage(new TestEventOne(), 3),
                    new ArgumentException("no"),
                    false);
            }
        }
        [Test]
        [TestCaseSource(nameof(ApplyWhenTestData))]
        public void ApplyWhenTest(FailedMessage message, Exception exception, bool mustApply)
        {
            var policy = (TestErrorPolicy)new TestErrorPolicy()
                .ApplyWhen((msg, ex) => msg.FailedAttempts <= 5 && ex.Message != "no");

            var canHandle = policy.CanHandle(message, exception);

            Assert.That(canHandle, Is.EqualTo(mustApply));
        }
    }
}