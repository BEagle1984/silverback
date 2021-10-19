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
using Silverback.Configuration;
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
        public static IEnumerable<object[]> HandleAsync_None_WarningIsNotLogged_TestData
        {
            get
            {
                yield return new object[]
                {
                    new TestValidationMessage
                    {
                        Id = "1",
                        String10 = "123456789abc",
                        IntRange = 5,
                        NumbersOnly = "123"
                    }
                };
                yield return new object[]
                {
                    new TestValidationMessage
                    {
                        Id = "1",
                        String10 = "123456",
                        IntRange = 30,
                        NumbersOnly = "123"
                    }
                };
                yield return new object[]
                {
                    new TestValidationMessage
                    {
                        String10 = "123456",
                        IntRange = 5,
                        NumbersOnly = "123"
                    }
                };
                yield return new object[]
                {
                    new TestValidationMessage
                    {
                        Id = "1",
                        String10 = "123456",
                        IntRange = 5,
                        NumbersOnly = "Test1234"
                    }
                };
            }
        }

        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "TestData")]
        public static IEnumerable<object[]>
            HandleAsync_MessageValidationModeLogWarning_WarningIsLogged_TestData
        {
            get
            {
                yield return new object[]
                {
                    new TestValidationMessage
                    {
                        Id = "1",
                        String10 = "123456789abc",
                        IntRange = 5,
                        NumbersOnly = "123"
                    },
                    $"An invalid message has been processed. | validation errors:{Environment.NewLine}- The field String10 must be a string with a maximum length of 10."
                };
                yield return new object[]
                {
                    new TestValidationMessage
                    {
                        Id = "1",
                        String10 = "123456",
                        IntRange = 30,
                        NumbersOnly = "123"
                    },
                    $"An invalid message has been processed. | validation errors:{Environment.NewLine}- The field IntRange must be between 5 and 10."
                };
                yield return new object[]
                {
                    new TestValidationMessage
                    {
                        Id = "1",
                        String10 = "123456789abc",
                        IntRange = 30,
                        NumbersOnly = "123"
                    },
                    $"An invalid message has been processed. | validation errors:{Environment.NewLine}- The field String10 must be a string with a maximum length of 10.{Environment.NewLine}- The field IntRange must be between 5 and 10."
                };
                yield return new object[]
                {
                    new TestValidationMessage
                    {
                        String10 = "123456",
                        IntRange = 5,
                        NumbersOnly = "123"
                    },
                    $"An invalid message has been processed. | validation errors:{Environment.NewLine}- The Id field is required."
                };
                yield return new object[]
                {
                    new TestValidationMessage
                    {
                        Id = "1",
                        String10 = "123456",
                        IntRange = 5,
                        NumbersOnly = "Test1234"
                    },
                    $"An invalid message has been processed. | validation errors:{Environment.NewLine}- The field NumbersOnly must match the regular expression '^[0-9]*$'."
                };
                yield return new object[]
                {
                    new TestValidationMessage
                    {
                        Id = "1",
                        String10 = "123456",
                        IntRange = 5,
                        NumbersOnly = "123",
                        Nested = new ValidationMessageNestedModel
                        {
                            String5 = "123456"
                        }
                    },
                    $"An invalid message has been processed. | validation errors:{Environment.NewLine}- The field String5 must be a string with a maximum length of 5."
                };
            }
        }

        [Theory]
        [MemberData(nameof(HandleAsync_None_WarningIsNotLogged_TestData))]
        public async Task HandleAsync_None_WarningIsNotLogged(IIntegrationMessage message)
        {
            TestConsumerEndpoint endpoint = new TestConsumerConfiguration("topic1")
            {
                MessageValidationMode = MessageValidationMode.None
            }.GetDefaultEndpoint();

            var envelope = new InboundEnvelope(
                message,
                null,
                null,
                new TestOffset(),
                endpoint);

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
            TestConsumerEndpoint endpoint = new TestConsumerConfiguration("topic1")
            {
                MessageValidationMode = validationMode
            }.GetDefaultEndpoint();

            var envelope = new InboundEnvelope(
                message,
                null,
                null,
                new TestOffset(),
                endpoint);

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
            TestConsumerEndpoint endpoint = new TestConsumerConfiguration("topic1")
            {
                MessageValidationMode = MessageValidationMode.LogWarning
            }.GetDefaultEndpoint();

            var envelope = new InboundEnvelope(
                message,
                null,
                null,
                new TestOffset(),
                endpoint);

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
            TestConsumerEndpoint endpoint = new TestConsumerConfiguration("topic1")
            {
                MessageValidationMode = MessageValidationMode.ThrowException
            }.GetDefaultEndpoint();
            var envelope = new InboundEnvelope(
                message,
                null,
                null,
                new TestOffset(),
                endpoint);

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
