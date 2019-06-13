// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling
{
    public class RetryErrorPolicyTests
    {
        private readonly RetryErrorPolicy _policy;

        public RetryErrorPolicyTests()
        {
            _policy = new RetryErrorPolicy(null, NullLoggerFactory.Instance.CreateLogger<RetryErrorPolicy>(), new MessageLogger(new MessageKeyProvider(new[] { new DefaultPropertiesMessageKeyProvider() })));
            _policy.MaxFailedAttempts(3);
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(3, true)]
        [InlineData(4, false)]
        [InlineData(7, false)]
        public void CanHandleTest(int failedAttempts, bool expectedResult)
        {
            var canHandle = _policy.CanHandle(new InboundMessage { Message = new TestEventOne(), FailedAttempts = failedAttempts }, new Exception("test"));

            canHandle.Should().Be(expectedResult);
        }
    }
}