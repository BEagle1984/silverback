// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Validation;

public class ValidatorProducerBehaviorTests
{
    private readonly LoggerSubstitute<ValidatorProducerBehavior> _loggerSubstitute;

    private readonly IProducerLogger<ValidatorProducerBehavior> _producerLogger;

    public ValidatorProducerBehaviorTests()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddLoggerSubstitute(LogLevel.Trace)
                .AddSilverback()
                .WithConnectionToMessageBroker());

        _loggerSubstitute =
            (LoggerSubstitute<ValidatorProducerBehavior>)serviceProvider
                .GetRequiredService<ILogger<ValidatorProducerBehavior>>();

        _producerLogger = serviceProvider
            .GetRequiredService<IProducerLogger<ValidatorProducerBehavior>>();
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "TestData")]
    [SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "Not working")]
    public static TheoryData<TestValidationMessage> HandleAsync_None_WarningIsNotLogged_TestData =>
        new()
        {
            new TestValidationMessage
            {
                Id = "1",
                String10 = "123456789abc",
                IntRange = 5,
                NumbersOnly = "123"
            },
            new TestValidationMessage
            {
                Id = "1", String10 = "123456", IntRange = 30, NumbersOnly = "123"
            },
            new TestValidationMessage
            {
                String10 = "123456", IntRange = 5, NumbersOnly = "123"
            },
            new TestValidationMessage
            {
                Id = "1", String10 = "123456", IntRange = 5, NumbersOnly = "Test1234"
            }
        };

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "TestData")]
    public static TheoryData<TestValidationMessage, string> HandleAsync_MessageValidationModeLogWarning_WarningIsLogged_TestData =>
        new()
        {
            {
                new TestValidationMessage
                {
                    Id = "1", String10 = "123456789abc", IntRange = 5, NumbersOnly = "123"
                },
                $"Invalid message produced:{Environment.NewLine}- The field String10 must be a string with a maximum length of 10."
            },
            {
                new TestValidationMessage
                {
                    Id = "1", String10 = "123456", IntRange = 30, NumbersOnly = "123"
                },
                $"Invalid message produced:{Environment.NewLine}- The field IntRange must be between 5 and 10."
            },
            {
                new TestValidationMessage
                {
                    Id = "1", String10 = "123456789abc", IntRange = 30, NumbersOnly = "123"
                },
                $"Invalid message produced:{Environment.NewLine}- The field String10 must be a string with a maximum length of 10.{Environment.NewLine}- The field IntRange must be between 5 and 10."
            },
            {
                new TestValidationMessage
                {
                    String10 = "123456", IntRange = 5, NumbersOnly = "123"
                },
                $"Invalid message produced:{Environment.NewLine}- The Id field is required."
            },
            {
                new TestValidationMessage
                {
                    Id = "1", String10 = "123456", IntRange = 5, NumbersOnly = "Test1234"
                },
                $"Invalid message produced:{Environment.NewLine}- The field NumbersOnly must match the regular expression '^[0-9]*$'."
            },
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
                $"Invalid message produced:{Environment.NewLine}- The field String5 must be a string with a maximum length of 5."
            }
        };

    [Theory]
    [MemberData(nameof(HandleAsync_None_WarningIsNotLogged_TestData))]
    public async Task HandleAsync_None_WarningIsNotLogged(IIntegrationMessage message)
    {
        TestProducerEndpoint endpoint = new TestProducerEndpointConfiguration("topic1")
        {
            MessageValidationMode = MessageValidationMode.None
        }.GetDefaultEndpoint();
        OutboundEnvelope envelope = new(message, null, endpoint, Substitute.For<IProducer>());

        IOutboundEnvelope? result = null;
        await new ValidatorProducerBehavior(_producerLogger).HandleAsync(
            new ProducerPipelineContext(
                envelope,
                Substitute.For<IProducer>(),
                Substitute.For<IServiceProvider>()),
            context =>
            {
                result = context.Envelope;
                return default;
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
        TestValidationMessage message = new() { Id = "1", String10 = "123", IntRange = 5, NumbersOnly = "123" };
        TestProducerEndpoint endpoint = new TestProducerEndpointConfiguration("topic1")
        {
            MessageValidationMode = validationMode
        }.GetDefaultEndpoint();
        OutboundEnvelope envelope = new(message, null, endpoint, Substitute.For<IProducer>());

        IOutboundEnvelope? result = null;
        Func<Task> act = () => new ValidatorProducerBehavior(_producerLogger).HandleAsync(
            new ProducerPipelineContext(
                envelope,
                Substitute.For<IProducer>(),
                Substitute.For<IServiceProvider>()),
            context =>
            {
                result = context.Envelope;
                return default;
            }).AsTask();

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
        TestProducerEndpoint endpoint = new TestProducerEndpointConfiguration("topic1")
        {
            MessageValidationMode = MessageValidationMode.LogWarning
        }.GetDefaultEndpoint();
        OutboundEnvelope envelope = new(message, null, endpoint, Substitute.For<IProducer>());

        IOutboundEnvelope? result = null;
        await new ValidatorProducerBehavior(_producerLogger).HandleAsync(
            new ProducerPipelineContext(
                envelope,
                Substitute.For<IProducer>(),
                Substitute.For<IServiceProvider>()),
            context =>
            {
                result = context.Envelope;
                return default;
            });

        result.Should().NotBeNull();
        result!.Message.Should().NotBeNull();
        expectedValidationMessage += " | endpointName: topic1, messageType: (null), messageId: (null), unused1: (null), unused2: (null)";
        _loggerSubstitute.Received(LogLevel.Warning, null, expectedValidationMessage, 1081);
    }

    [Fact]
    public async Task HandleAsync_ThrowException_ExceptionIsThrown()
    {
        TestValidationMessage message = new() { Id = "1", String10 = "123456789abc", IntRange = 5, NumbersOnly = "123" };
        string expectedMessage =
            $"The message is not valid:{Environment.NewLine}- The field String10 must be a string with a maximum length of 10.";
        TestProducerEndpoint endpoint = new TestProducerEndpointConfiguration("topic1")
        {
            MessageValidationMode = MessageValidationMode.ThrowException
        }.GetDefaultEndpoint();
        OutboundEnvelope envelope = new(message, null, endpoint, Substitute.For<IProducer>());

        IOutboundEnvelope? result = null;
        Func<Task> act = () => new ValidatorProducerBehavior(_producerLogger).HandleAsync(
            new ProducerPipelineContext(
                envelope,
                Substitute.For<IProducer>(),
                Substitute.For<IServiceProvider>()),
            context =>
            {
                result = context.Envelope;
                return default;
            }).AsTask();

        result.Should().BeNull();
        await act.Should().ThrowAsync<MessageValidationException>().WithMessage(expectedMessage);
    }
}
