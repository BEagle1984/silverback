// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling
{
    public class RetryErrorPolicyTests
    {
        private readonly ErrorPolicyBuilder _errorPolicyBuilder;
        private readonly IBroker _broker;

        public RetryErrorPolicyTests()
        {
            var services = new ServiceCollection();

            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            services.AddBus();

            services.AddBroker<TestBroker>(options => { });

            var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

            _errorPolicyBuilder = new ErrorPolicyBuilder(serviceProvider, NullLoggerFactory.Instance);

            _broker = serviceProvider.GetRequiredService<IBroker>();
            _broker.Connect();
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(3, true)]
        [InlineData(4, false)]
        [InlineData(7, false)]
        public void CanHandle_InboundMessageWithDifferentFailedAttemptsCount_ReturnReflectsMaxFailedAttempts(int failedAttempts, bool expectedResult)
        {
            var policy = _errorPolicyBuilder.Retry().MaxFailedAttempts(3);

            var canHandle = policy.CanHandle(new InboundMessage { Message = new TestEventOne(), FailedAttempts = failedAttempts }, new Exception("test"));

            canHandle.Should().Be(expectedResult);
        }
    }
}