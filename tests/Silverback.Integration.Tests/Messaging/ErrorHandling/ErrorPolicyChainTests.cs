// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling
{
    public class ErrorPolicyChainTests
    {
        private readonly ErrorPolicyBuilder _errorPolicyBuilder;

        public ErrorPolicyChainTests()
        {
            var services = new ServiceCollection();

            services.AddBus().AddBroker<TestBroker>();

            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            _errorPolicyBuilder = new ErrorPolicyBuilder(services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true }), NullLoggerFactory.Instance);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4)]
        public void ChainingTest(int failedAttempts)
        {
            var testPolicy = new TestErrorPolicy();

            var chain = _errorPolicyBuilder.Chain(
                _errorPolicyBuilder.Retry().MaxFailedAttempts(3),
                testPolicy);

            chain.HandleError(new FailedMessage(new TestEventOne(), failedAttempts), new Exception("test"));

            testPolicy.Applied.Should().Be(failedAttempts > 3);
        }

        [Theory]
        [InlineData(1, ErrorAction.Retry)]
        [InlineData(2, ErrorAction.Retry)]
        [InlineData(3, ErrorAction.Skip)]
        [InlineData(4, ErrorAction.Skip)]
        public void ChainingTest2(int failedAttempts, ErrorAction expectedAction)
        {
            var chain = _errorPolicyBuilder.Chain(
                _errorPolicyBuilder.Retry().MaxFailedAttempts(2),
                _errorPolicyBuilder.Skip());

            var action = chain.HandleError(new FailedMessage(new TestEventOne(), failedAttempts), new Exception("test"));

            action.Should().Be(expectedAction);
        }
    }
}
