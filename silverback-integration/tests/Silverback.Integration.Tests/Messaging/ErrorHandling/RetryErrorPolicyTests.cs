// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

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
        [TestCase(1, true)]
        [TestCase(3, false)]
        [TestCase(4, false)]
        public void CanHandleTest(int failedAttempts, bool expectedResult)
        {
            var canHandle = _policy.CanHandle(new FailedMessage(new TestEventOne(), failedAttempts), new Exception("test"));

            Assert.That(canHandle, Is.EqualTo(expectedResult));
        }
    }
}