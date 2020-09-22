// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling
{
    public class RetryErrorPolicyTests
    {
        private readonly ServiceProvider _serviceProvider;

        public RetryErrorPolicyTests()
        {
            var services = new ServiceCollection();

            services
                .AddNullLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>());

            _serviceProvider = services.BuildServiceProvider();

            var broker = _serviceProvider.GetRequiredService<IBroker>();
            broker.Connect();
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(3, true)]
        [InlineData(4, false)]
        [InlineData(7, false)]
        public void CanHandle_WithDifferentFailedAttemptsCount_ReturnReflectsMaxFailedAttempts(
            int failedAttempts,
            bool expectedResult)
        {
            var policy = ErrorPolicy.Retry().MaxFailedAttempts(3).Build(_serviceProvider);

            var rawMessage = new MemoryStream();
            var headers = new[]
            {
                new MessageHeader(DefaultMessageHeaders.FailedAttempts, failedAttempts)
            };

            var inboundEnvelope = new InboundEnvelope(
                rawMessage,
                headers,
                null,
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name);

            var canHandle = policy.CanHandle(
                ConsumerPipelineContextHelper.CreateSubstitute(inboundEnvelope, _serviceProvider),
                new InvalidOperationException("test"));

            canHandle.Should().Be(expectedResult);
        }

        [Fact]
        public async Task HandleError_Whatever_TrueReturned()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task HandleError_Whatever_OffsetRolledBack()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task HandleError_Whatever_TransactionAborted()
        {
            throw new NotImplementedException();
        }
    }
}
