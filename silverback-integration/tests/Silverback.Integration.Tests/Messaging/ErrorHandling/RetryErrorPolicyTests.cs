using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.ErrorHandling
{
    [TestFixture]
    public class RetryErrorPolicyTests
    {
        private RetryErrorPolicy _policy;

        [SetUp]
        public void Setup()
        {
            _policy = new RetryErrorPolicy(NullLoggerFactory.Instance.CreateLogger<RetryErrorPolicy>(), 3);
        }

        [Test]
        public void SuccessTest()
        {
            var executed = false;
            _policy.TryHandleMessage(
                Envelope.Create(new TestEventOne()),
                _ => executed = true);

            Assert.That(executed, Is.True);
        }

        [Test]
        public void SuccessAfterRetryTest()
        {
            var tryCount = 0;
            var success = false;

            _policy.TryHandleMessage(
                Envelope.Create(new TestEventOne()),
                _ =>
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
                _policy.TryHandleMessage(
                    Envelope.Create(new TestEventOne()),
                    _ =>
                    {
                        tryCount++;
                        throw new Exception("retry, please");
                    }));

            Assert.That(tryCount, Is.EqualTo(4));
        }
    }
}