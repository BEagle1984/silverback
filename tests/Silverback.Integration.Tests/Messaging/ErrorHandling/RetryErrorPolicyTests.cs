﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Inbound.Transaction;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Types;
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
            broker.ConnectAsync().Wait();
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

            var envelope = new InboundEnvelope(
                rawMessage,
                headers,
                new TestOffset(),
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name);

            var canHandle = policy.CanHandle(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
                new InvalidOperationException("test"));

            canHandle.Should().Be(expectedResult);
        }

        [Fact]
        public async Task HandleErrorAsync_Whatever_TrueReturned()
        {
            var policy = ErrorPolicy.Retry().MaxFailedAttempts(3).Build(_serviceProvider);
            var envelope = new InboundEnvelope(
                "hey oh!",
                new MemoryStream(),
                null,
                new TestOffset(),
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name);

            var result = await policy.HandleErrorAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
                new InvalidOperationException("test"));

            result.Should().BeTrue();
        }

        [Fact]
        public async Task HandleErrorAsync_Whatever_ConsumerRolledBackAndTransactionAborted()
        {
            var policy = ErrorPolicy.Retry().MaxFailedAttempts(3).Build(_serviceProvider);
            var envelope = new InboundEnvelope(
                "hey oh!",
                new MemoryStream(),
                null,
                new TestOffset(),
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name);

            var transactionManager = Substitute.For<IConsumerTransactionManager>();

            await policy.HandleErrorAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider, transactionManager),
                new InvalidOperationException("test"));

            await transactionManager.Received(1).RollbackAsync(
                Arg.Any<InvalidOperationException>(),
                false,
                true,
                false);
        }
    }
}
