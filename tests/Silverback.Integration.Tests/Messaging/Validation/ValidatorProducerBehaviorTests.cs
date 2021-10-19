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
using NSubstitute;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Validation;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Validation
{
    public class ValidatorProducerBehaviorTests
    {
        private readonly LoggerSubstitute<ValidatorProducerBehavior> _loggerSubstitute;

        private readonly IOutboundLogger<ValidatorProducerBehavior> _outboundLogger;

        public ValidatorProducerBehaviorTests()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddLoggerSubstitute(LogLevel.Trace)
                    .AddSilverback()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddBroker<TestBroker>()));

            _loggerSubstitute =
                (LoggerSubstitute<ValidatorProducerBehavior>)serviceProvider
                    .GetRequiredService<ILogger<ValidatorProducerBehavior>>();

            _outboundLogger = serviceProvider
                .GetRequiredService<IOutboundLogger<ValidatorProducerBehavior>>();
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
                    $"An invalid message has been produced. | validation errors:{Environment.NewLine}- The field String10 must be a string with a maximum length of 10."
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
                    $"An invalid message has been produced. | validation errors:{Environment.NewLine}- The field IntRange must be between 5 and 10."
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
                    $"An invalid message has been produced. | validation errors:{Environment.NewLine}- The field String10 must be a string with a maximum length of 10.{Environment.NewLine}- The field IntRange must be between 5 and 10."
                };
                yield return new object[]
                {
                    new TestValidationMessage
                    {
                        String10 = "123456",
                        IntRange = 5,
                        NumbersOnly = "123"
                    },
                    $"An invalid message has been produced. | validation errors:{Environment.NewLine}- The Id field is required."
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
                    $"An invalid message has been produced. | validation errors:{Environment.NewLine}- The field NumbersOnly must match the regular expression '^[0-9]*$'."
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
                    $"An invalid message has been produced. | validation errors:{Environment.NewLine}- The field String5 must be a string with a maximum length of 5."
                };
            }
        }

        [Theory]
        [MemberData(nameof(HandleAsync_None_WarningIsNotLogged_TestData))]
        public async Task HandleAsync_None_WarningIsNotLogged(IIntegrationMessage message)
        {
            TestProducerEndpoint endpoint = new TestProducerConfiguration("topic1")
            {
                MessageValidationMode = MessageValidationMode.None
            }.GetDefaultEndpoint();
            var envelope = new OutboundEnvelope(message, null, endpoint);

            IOutboundEnvelope? result = null;
            await new ValidatorProducerBehavior(_outboundLogger).HandleAsync(
                new ProducerPipelineContext(
                    envelope,
                    Substitute.For<IProducer>(),
                    Substitute.For<IServiceProvider>()),
                context =>
                {
                    result = context.Envelope;
                    return Task.CompletedTask;
                });

            result.Should().NotBeNull();
            result!.Message.Should().NotBeNull();
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
            TestProducerEndpoint endpoint = new TestProducerConfiguration("topic1")
            {
                MessageValidationMode = validationMode
            }.GetDefaultEndpoint();
            var envelope = new OutboundEnvelope(message, null, endpoint);

            IOutboundEnvelope? result = null;
            Func<Task> act = () => new ValidatorProducerBehavior(_outboundLogger).HandleAsync(
                new ProducerPipelineContext(
                    envelope,
                    Substitute.For<IProducer>(),
                    Substitute.For<IServiceProvider>()),
                context =>
                {
                    result = context.Envelope;
                    return Task.CompletedTask;
                });

            await act.Should().NotThrowAsync<ValidationException>();
            result.Should().NotBeNull();
            result!.Message.Should().NotBeNull();
            _loggerSubstitute.DidNotReceive(LogLevel.Warning, null).Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(HandleAsync_MessageValidationModeLogWarning_WarningIsLogged_TestData))]
        public async Task HandleAsync_LogWarning_WarningIsLogged(
            IIntegrationMessage message,
            string expectedValidationMessage)
        {
            TestProducerEndpoint endpoint = new TestProducerConfiguration("topic1")
            {
                MessageValidationMode = MessageValidationMode.LogWarning
            }.GetDefaultEndpoint();
            var envelope = new OutboundEnvelope(message, null, endpoint);

            IOutboundEnvelope? result = null;
            await new ValidatorProducerBehavior(_outboundLogger).HandleAsync(
                new ProducerPipelineContext(
                    envelope,
                    Substitute.For<IProducer>(),
                    Substitute.For<IServiceProvider>()),
                context =>
                {
                    result = context.Envelope;
                    return Task.CompletedTask;
                });

            result.Should().NotBeNull();
            result!.Message.Should().NotBeNull();
            _loggerSubstitute.Received(LogLevel.Warning, null, expectedValidationMessage, 1079);
        }

        [Fact]
        public async Task HandleAsync_ThrowException_ExceptionIsThrown()
        {
            var message = new TestValidationMessage
                { Id = "1", String10 = "123456789abc", IntRange = 5, NumbersOnly = "123" };
            var expectedMessage =
                $"The message is not valid:{Environment.NewLine}- The field String10 must be a string with a maximum length of 10.";
            TestProducerEndpoint endpoint = new TestProducerConfiguration("topic1")
            {
                MessageValidationMode = MessageValidationMode.ThrowException
            }.GetDefaultEndpoint();
            var envelope = new OutboundEnvelope(message, null, endpoint);

            IOutboundEnvelope? result = null;
            Func<Task> act = () => new ValidatorProducerBehavior(_outboundLogger).HandleAsync(
                new ProducerPipelineContext(
                    envelope,
                    Substitute.For<IProducer>(),
                    Substitute.For<IServiceProvider>()),
                context =>
                {
                    result = context.Envelope;
                    return Task.CompletedTask;
                });

            result.Should().BeNull();
            await act.Should().ThrowAsync<MessageValidationException>().WithMessage(expectedMessage);
        }
    }
}
