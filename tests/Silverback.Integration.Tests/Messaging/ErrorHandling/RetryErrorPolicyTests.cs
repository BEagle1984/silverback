// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling
{
    public class RetryErrorPolicyTests
    {
        private readonly ErrorPolicyBuilder _errorPolicyBuilder;

        public RetryErrorPolicyTests()
        {
            var services = new ServiceCollection();

            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            services.AddSilverback().WithConnectionToMessageBroker(options => options
                .AddBroker<TestBroker>());

            var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

            _errorPolicyBuilder = new ErrorPolicyBuilder(serviceProvider, NullLoggerFactory.Instance);

            var broker = serviceProvider.GetRequiredService<IBroker>();
            broker.Connect();
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(3, true)]
        [InlineData(4, false)]
        [InlineData(7, false)]
        public void CanHandle_InboundMessageWithDifferentFailedAttemptsCount_ReturnReflectsMaxFailedAttempts(
            int failedAttempts,
            bool expectedResult)
        {
            var policy = _errorPolicyBuilder.Retry().MaxFailedAttempts(3);

            var canHandle = policy.CanHandle(new[]
            {
                new InboundEnvelope(
                    new byte[1],
                    new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, failedAttempts.ToString()) },
                    null, TestConsumerEndpoint.GetDefault(),
                    TestConsumerEndpoint.GetDefault().Name),
            }, new Exception("test"));

            canHandle.Should().Be(expectedResult);
        }
    }
}