using System;
using System.Linq;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;
using Silverback.Messaging.Configuration;

namespace Silverback.Tests.Messaging.ErrorHandling
{
    [TestFixture]
    public class ErrorPolicyChainTests
    {
        [Test]
        public void ChainingTest()
        {
            var testPolicies = new[]
            {
                new TestErrorPolicy(),
                new TestErrorPolicy(),
                new TestErrorPolicy(),
                new TestErrorPolicy(),
                new TestErrorPolicy()
            };

            var chain = ErrorPolicy.Chain(testPolicies);
            chain.Init(new BusBuilder().Build());

            chain.TryHandleMessage(
                Envelope.Create(new TestEventOne()),
                _ => throw new Exception("retry, please"));

            Assert.That(testPolicies.Count(x => x.Applied), Is.EqualTo(1));
        }

        [Test]
        public void ChainingTest2()
        {
            var testPolicy = new TestErrorPolicy();

            var chain = ErrorPolicy.Chain(
                ErrorPolicy.Retry(1),
                ErrorPolicy.Retry(1),
                ErrorPolicy.Retry(1),
                ErrorPolicy.Retry(1), 
                testPolicy);

            chain.Init(new BusBuilder().Build());

            chain.TryHandleMessage(
                Envelope.Create(new TestEventOne()),
                _ => throw new Exception("retry, please"));

            Assert.That(testPolicy.Applied);
        }

        [Test]
        public void ChainingTest3()
        {
            var tryCount = 0;

            var testPolicy = new TestErrorPolicy();
            var chain = ErrorPolicy.Chain(ErrorPolicy.Retry(3), testPolicy);
            chain.Init(new BusBuilder().Build());

            chain.TryHandleMessage(
                Envelope.Create(new TestEventOne()),
                _ =>
                {
                    tryCount++;
                    throw new Exception("retry, please");
                });

            Assert.That(testPolicy.Applied, Is.True);
            Assert.That(tryCount, Is.EqualTo(4));
        }

        [Test]
        public void ChainingTest4()
        {
            var tryCount = 0;

            var chain = ErrorPolicy.Chain(ErrorPolicy.Retry(3), ErrorPolicy.Skip());
            chain.Init(new BusBuilder().Build());

            chain.TryHandleMessage(
                Envelope.Create(new TestEventOne()),
                _ =>
                {
                    tryCount++;
                    throw new Exception("retry, please");
                });

            Assert.That(tryCount, Is.EqualTo(4));
        }

        [Test]
        public void ChainingTest5()
        {
            var tryCount = 0;

            var chain = ErrorPolicy.Chain(ErrorPolicy.Retry(3).ApplyTo<InvalidOperationException>(), ErrorPolicy.Skip());
            chain.Init(new BusBuilder().Build());

            chain.TryHandleMessage(
                Envelope.Create(new TestEventOne()),
                _ =>
                {
                    tryCount++;
                    if (tryCount < 2)
                        throw new InvalidOperationException();
                    throw new Exception("retry, please");
                });

            Assert.That(tryCount, Is.EqualTo(2));
        }
    }
}
