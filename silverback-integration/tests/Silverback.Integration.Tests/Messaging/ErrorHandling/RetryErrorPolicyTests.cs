using System;
using NUnit.Framework;
using Silverback.Messaging.ErrorHandling;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.ErrorHandling
{
    [TestFixture]
    public class RetryErrorPolicyTests
    {
        [Test]
        public void SuccessTest()
        {
            var executed = false;
            new NoErrorPolicy().TryHandleMessage(new TestEventOne(), _ => executed = true);

            Assert.That(executed, Is.True);
        }

        [Test]
        public void SuccessAfterRetryTest()
        {
            var tryCount = 0;
            var success = false;

            new RetryErrorPolicy(5).TryHandleMessage(new TestEventOne(), _ =>
            {
                if (++tryCount < 3)
                    throw new Exception("retry, please");

                success = true;
            });

            Assert.That(success, Is.True);
            Assert.That(tryCount, Is.EqualTo(3));
        }

        [Test]
        public void ErrorTest()
        {
            var tryCount = 0;

            Assert.Throws<ErrorPolicyException>(() =>
                new RetryErrorPolicy(3).TryHandleMessage(new TestEventOne(), _ =>
                {
                    tryCount++;
                    throw new Exception("retry, please");
                }));

            Assert.That(tryCount, Is.EqualTo(4));
        }

        [Test]
        public void ChainingTest()
        {
            var tryCount = 0;

            var testPolicy = new TestErrorPolicy();

            new RetryErrorPolicy(1)
                .Wrap(testPolicy)
                .TryHandleMessage(new TestEventOne(), _ =>
                {
                    tryCount++;
                    throw new Exception("retry, please");
                });

            Assert.That(testPolicy.Applied, Is.True);
            Assert.That(tryCount, Is.EqualTo(2));
        }
    }
}