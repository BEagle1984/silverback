// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Messaging.ErrorHandling
{
    public class RetryErrorPolicyTests
    {
        private RetryErrorPolicy _policy;

        public RetryErrorPolicyTests()
        {
            _policy = new RetryErrorPolicy(NullLoggerFactory.Instance.CreateLogger<RetryErrorPolicy>());
            _policy.MaxFailedAttempts(3);
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(3, true)]
        [InlineData(4, false)]
        [InlineData(7, false)]
        public void CanHandleTest(int failedAttempts, bool expectedResult)
        {
            var canHandle = _policy.CanHandle(new FailedMessage(new TestEventOne(), failedAttempts), new Exception("test"));

            canHandle.Should().Be(expectedResult);
        }
    }
}