using System;
using System.Collections.Generic;
using NUnit.Framework;
using Silverback.Messaging.Configuration;
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
            var policy = (TestErrorPolicy)new TestErrorPolicy()
                .ApplyTo<ArgumentException>()
                .ApplyTo<InvalidCastException>();

            policy.Init(new BusBuilder().Build());

            var exceptionThrown = false;

            try
            {
                policy.TryHandleMessage(
                    Envelope.Create(new TestEventOne()),
                    _ => throw exception);
            }
            catch
            {
                exceptionThrown = true;
            }

            Assert.That(policy.Applied, Is.EqualTo(mustApply));
            Assert.That(exceptionThrown, Is.Not.EqualTo(mustApply));
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

            policy.Init(new BusBuilder().Build());

            var exceptionThrown = false;

            try
            {
                policy.TryHandleMessage(
                    Envelope.Create(new TestEventOne()),
                    _ => throw exception);
            }
            catch
            {
                exceptionThrown = true;
            }

            Assert.That(policy.Applied, Is.EqualTo(mustApply));
            Assert.That(exceptionThrown, Is.Not.EqualTo(mustApply));
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

            policy.Init(new BusBuilder().Build());

            var exceptionThrown = false;

            try
            {
                policy.TryHandleMessage(
                    Envelope.Create(new TestEventOne()),
                    _ => throw exception);
            }
            catch
            {
                exceptionThrown = true;
            }

            Assert.That(policy.Applied, Is.EqualTo(mustApply));
            Assert.That(exceptionThrown, Is.Not.EqualTo(mustApply));
        }
    }
}