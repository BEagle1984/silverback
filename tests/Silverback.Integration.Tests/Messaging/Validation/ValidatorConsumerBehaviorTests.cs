// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Validation;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Validation
{
    public class ValidatorConsumerBehaviorTests
    {
        private readonly ServiceProvider _serviceProvider;

        private readonly LoggerSubstitute<ValidatorConsumerBehavior> _loggerSubstitute;

        private readonly IInboundLogger<ValidatorConsumerBehavior> _inboundLogger;

        public ValidatorConsumerBehaviorTests()
        {
            var services = new ServiceCollection();

            services
                .AddLoggerSubstitute()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>());

            _serviceProvider = services.BuildServiceProvider();

            _loggerSubstitute =
                (LoggerSubstitute<ValidatorConsumerBehavior>)_serviceProvider
                    .GetRequiredService<ILogger<ValidatorConsumerBehavior>>();

            _inboundLogger = _serviceProvider
                .GetRequiredService<IInboundLogger<ValidatorConsumerBehavior>>();
        }

        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "TestData")]
        public static IEnumerable<object[]> HandleAsync_None_WarningIsNotLogged_TestData =>
            new[]
            {
                new object[]
                {
                    new TestValidationMessage
                        { Id = "1", String10 = "123456789abc", IntRange = 5, NumbersOnly = "123" }
                },
                new object[]
                {
                    new TestValidationMessage
                        { Id = "1", String10 = "123456", IntRange = 30, NumbersOnly = "123" }
                },
                new object[]
                {
                    new TestValidationMessage { String10 = "123456", IntRange = 5, NumbersOnly = "123" }
                },
                new object[]
                {
                    new TestValidationMessage
                        { Id = "1", String10 = "123456", IntRange = 5, NumbersOnly = "Test1234" }
                }
            };

        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "TestData")]
        public static IEnumerable<object[]>
            HandleAsync_MessageValidationModeLogWarning_WarningIsLogged_TestData =>
            new[]
            {
                new object[]
                {
                    new TestValidationMessage
                        { Id = "1", String10 = "123456789abc", IntRange = 5, NumbersOnly = "123" },
                    $"An invalid message has been processed. | validation errors:{Environment.NewLine}- The field String10 must be a string with a maximum length of 10."
                },
                new object[]
                {
                    new TestValidationMessage
                        { Id = "1", String10 = "123456", IntRange = 30, NumbersOnly = "123" },
                    $"An invalid message has been processed. | validation errors:{Environment.NewLine}- The field IntRange must be between 5 and 10."
                },
                new object[]
                {
                    new TestValidationMessage
                        { Id = "1", String10 = "123456789abc", IntRange = 30, NumbersOnly = "123" },
                    $"An invalid message has been processed. | validation errors:{Environment.NewLine}- The field String10 must be a string with a maximum length of 10.{Environment.NewLine}- The field IntRange must be between 5 and 10."
                },
                new object[]
                {
                    new TestValidationMessage { String10 = "123456", IntRange = 5, NumbersOnly = "123" },
                    $"An invalid message has been processed. | validation errors:{Environment.NewLine}- The Id field is required."
                },
                new object[]
                {
                    new TestValidationMessage
                        { Id = "1", String10 = "123456", IntRange = 5, NumbersOnly = "Test1234" },
                    $"An invalid message has been processed. | validation errors:{Environment.NewLine}- The field NumbersOnly must match the regular expression '^[0-9]*$'."
                }
            };

        [Theory]
        [MemberData(nameof(HandleAsync_None_WarningIsNotLogged_TestData))]
        public async Task HandleAsync_None_WarningIsNotLogged(IIntegrationMessage message)
        {
            var endpoint = TestConsumerEndpoint.GetDefault();
            endpoint.MessageValidationMode = MessageValidationMode.None;

            var envelope = new InboundEnvelope(
                message,
                null,
                null,
                new TestOffset(),
                endpoint,
                "source-endpoint");

            IRawInboundEnvelope? result = null;
            await new ValidatorConsumerBehavior(_inboundLogger).HandleAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
                context =>
                {
                    result = context.Envelope;
                    return Task.CompletedTask;
                });

            result.Should().NotBeNull();
            _loggerSubstitute.DidNotReceive(LogLevel.Warning, null).Should().BeTrue();
        }

        [Theory]
        [InlineData(MessageValidationMode.None)]
        [InlineData(MessageValidationMode.LogWarning)]
        [InlineData(MessageValidationMode.ThrowException)]
        public async Task HandleAsync_ValidMessage_NoLogAndNoException(MessageValidationMode validationMode)
        {
            var message = new TestValidationMessage
                { Id = "1", String10 = "123", IntRange = 5, NumbersOnly = "123" };
            var endpoint = TestConsumerEndpoint.GetDefault();
            endpoint.MessageValidationMode = validationMode;

            var envelope = new InboundEnvelope(
                message,
                null,
                null,
                new TestOffset(),
                endpoint,
                "source-endpoint");

            IRawInboundEnvelope? result = null;
            Func<Task> act = () => new ValidatorConsumerBehavior(_inboundLogger).HandleAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
                context =>
                {
                    result = context.Envelope;
                    return Task.CompletedTask;
                });
            await act.Should().NotThrowAsync<ValidationException>();
            result.Should().NotBeNull();
            _loggerSubstitute.DidNotReceive(LogLevel.Warning, null).Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(HandleAsync_MessageValidationModeLogWarning_WarningIsLogged_TestData))]
        public async Task HandleAsync_LogWarning_WarningIsLogged(
            IIntegrationMessage message,
            string expectedValidationMessage)
        {
            var endpoint = TestConsumerEndpoint.GetDefault();
            endpoint.MessageValidationMode = MessageValidationMode.LogWarning;

            var envelope = new InboundEnvelope(
                message,
                null,
                null,
                new TestOffset(),
                endpoint,
                "source-endpoint");

            IRawInboundEnvelope? result = null;
            await new ValidatorConsumerBehavior(_inboundLogger).HandleAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
                context =>
                {
                    result = context.Envelope;
                    return Task.CompletedTask;
                });

            result.Should().NotBeNull();
            _loggerSubstitute.Received(LogLevel.Warning, null, expectedValidationMessage, 1080);
        }

        [Fact]
        public async Task HandleAsync_ThrowException_ExceptionIsThrown()
        {
            var message = new TestValidationMessage
                { Id = "1", String10 = "123456789abc", IntRange = 5, NumbersOnly = "123" };
            var expectedMessage =
                $"The message is not valid:{Environment.NewLine}- The field String10 must be a string with a maximum length of 10.";
            var endpoint = TestConsumerEndpoint.GetDefault();
            endpoint.MessageValidationMode = MessageValidationMode.ThrowException;

            var envelope = new InboundEnvelope(
                message,
                null,
                null,
                new TestOffset(),
                endpoint,
                "source-endpoint");

            IRawInboundEnvelope? result = null;
            Func<Task> act = () => new ValidatorConsumerBehavior(_inboundLogger).HandleAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
                context =>
                {
                    result = context.Envelope;
                    return Task.CompletedTask;
                });

            await act.Should().ThrowAsync<MessageValidationException>().WithMessage(expectedMessage);
            result.Should().BeNull();
        }
    }
}
