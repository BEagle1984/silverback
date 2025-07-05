// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Validation;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Validation;

public class ValidatorConsumerBehaviorFixture
{
    private readonly ServiceProvider _serviceProvider;

    private readonly LoggerSubstitute<ValidatorConsumerBehavior> _loggerSubstitute;

    private readonly ISilverbackLogger<ValidatorConsumerBehavior> _logger;

    public ValidatorConsumerBehaviorFixture()
    {
        ServiceCollection services = [];

        services
            .AddLoggerSubstitute()
            .AddSilverback()
            .WithConnectionToMessageBroker();

        _serviceProvider = services.BuildServiceProvider();

        _loggerSubstitute =
            (LoggerSubstitute<ValidatorConsumerBehavior>)_serviceProvider
                .GetRequiredService<ILogger<ValidatorConsumerBehavior>>();

        _logger = _serviceProvider
            .GetRequiredService<ISilverbackLogger<ValidatorConsumerBehavior>>();
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "TestData")]
    [SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "Not working")]
    public static TheoryData<TestValidationMessage> HandleAsync_ShouldNotLog_WhenModeNone_TestData =>
    [
        new()
        {
            Id = "1",
            String10 = "123456789abc",
            IntRange = 5,
            NumbersOnly = "123"
        },

        new()
        {
            Id = "1", String10 = "123456", IntRange = 30, NumbersOnly = "123"
        },

        new()
        {
            String10 = "123456", IntRange = 5, NumbersOnly = "123"
        },

        new()
        {
            Id = "1", String10 = "123456", IntRange = 5, NumbersOnly = "Test1234"
        }
    ];

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "TestData")]
    public static TheoryData<TestValidationMessage, string> HandleAsync_ShouldLogWarning_WhenModeIsLogWarning_TestData =>
        new()
        {
            {
                new TestValidationMessage
                {
                    Id = "1", String10 = "123456789abc", IntRange = 5, NumbersOnly = "123"
                },
                $"Invalid message consumed: {Environment.NewLine}- The field String10 must be a string with a maximum length of 10."
            },
            {
                new TestValidationMessage
                {
                    Id = "1", String10 = "123456", IntRange = 30, NumbersOnly = "123"
                },
                $"Invalid message consumed: {Environment.NewLine}- The field IntRange must be between 5 and 10."
            },
            {
                new TestValidationMessage
                {
                    Id = "1", String10 = "123456789abc", IntRange = 30, NumbersOnly = "123"
                },
                $"Invalid message consumed: {Environment.NewLine}- The field String10 must be a string with a maximum length of 10.{Environment.NewLine}- The field IntRange must be between 5 and 10."
            },
            {
                new TestValidationMessage
                {
                    String10 = "123456", IntRange = 5, NumbersOnly = "123"
                },
                $"Invalid message consumed: {Environment.NewLine}- The Id field is required."
            },
            {
                new TestValidationMessage
                {
                    Id = "1", String10 = "123456", IntRange = 5, NumbersOnly = "Test1234"
                },
                $"Invalid message consumed: {Environment.NewLine}- The field NumbersOnly must match the regular expression '^[0-9]*$'."
            },
            {
                new TestValidationMessage
                {
                    Id = "1",
                    String10 = "123456",
                    IntRange = 5,
                    NumbersOnly = "123",
                    FirstNested = new ValidationMessageNestedModel
                    {
                        String5 = "123456"
                    }
                },
                $"Invalid message consumed: {Environment.NewLine}- The field String5 must be a string with a maximum length of 5."
            },
            {
                new TestValidationMessage
                {
                    Id = "1",
                    String10 = "123456",
                    IntRange = 5,
                    NumbersOnly = "123",
                    FirstNested = new ValidationMessageNestedModel
                    {
                        String5 = "123456"
                    },
                    SecondNested = new ValidationMessageNestedModel
                    {
                        String5 = "12345"
                    }
                },
                $"Invalid message consumed: {Environment.NewLine}- The field String5 must be a string with a maximum length of 5."
            },
            {
                new TestValidationMessage
                {
                    Id = "1",
                    String10 = "123456",
                    IntRange = 5,
                    NumbersOnly = "123",
                    FirstNested = new ValidationMessageNestedModel
                    {
                        String5 = "123456"
                    },
                    SecondNested = new ValidationMessageNestedModel
                    {
                        String5 = "123456"
                    }
                },
                $"Invalid message consumed: {Environment.NewLine}- The field String5 must be a string with a maximum length of 5.{Environment.NewLine}- The field String5 must be a string with a maximum length of 5."
            }
        };

    [Theory]
    [MemberData(nameof(HandleAsync_ShouldNotLog_WhenModeNone_TestData))]
    public async Task HandleAsync_ShouldNotLog_WhenModeNone(IIntegrationMessage message)
    {
        TestConsumerEndpoint endpoint = new TestConsumerEndpointConfiguration("topic1")
        {
            MessageValidationMode = MessageValidationMode.None
        }.GetDefaultEndpoint();

        InboundEnvelope envelope = new(
            message,
            null,
            null,
            endpoint,
            Substitute.For<IConsumer>(),
            new TestOffset());

        IRawInboundEnvelope? result = null;
        await new ValidatorConsumerBehavior(_logger).HandleAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
            (context, _) =>
            {
                result = context.Envelope;
                return default;
            },
            CancellationToken.None);

        result.ShouldNotBeNull();
        _loggerSubstitute.DidNotReceive(LogLevel.Warning, null);
    }

    [Theory]
    [InlineData(MessageValidationMode.None)]
    [InlineData(MessageValidationMode.LogWarning)]
    [InlineData(MessageValidationMode.ThrowException)]
    public async Task HandleAsync_ShouldNotLogOrThrow_WhenMessageIsValid(MessageValidationMode validationMode)
    {
        TestValidationMessage message = new() { Id = "1", String10 = "123", IntRange = 5, NumbersOnly = "123" };
        TestConsumerEndpoint endpoint = new TestConsumerEndpointConfiguration("topic1")
        {
            MessageValidationMode = validationMode
        }.GetDefaultEndpoint();

        InboundEnvelope envelope = new(
            message,
            null,
            null,
            endpoint,
            Substitute.For<IConsumer>(),
            new TestOffset());

        IRawInboundEnvelope? result = null;
        Func<Task> act = () => new ValidatorConsumerBehavior(_logger).HandleAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
            (context, _) =>
            {
                result = context.Envelope;
                return default;
            },
            CancellationToken.None).AsTask();
        await act.ShouldNotThrowAsync();
        result.ShouldNotBeNull();
        _loggerSubstitute.DidNotReceive(LogLevel.Warning, null);
    }

    [Theory]
    [MemberData(nameof(HandleAsync_ShouldLogWarning_WhenModeIsLogWarning_TestData))]
    public async Task HandleAsync_ShouldLogWarning_WhenModeIsLogWarning(IIntegrationMessage message, string expectedValidationMessage)
    {
        TestConsumerEndpoint endpoint = new TestConsumerEndpointConfiguration("topic1")
        {
            MessageValidationMode = MessageValidationMode.LogWarning
        }.GetDefaultEndpoint();

        InboundEnvelope envelope = new(
            message,
            null,
            null,
            endpoint,
            Substitute.For<IConsumer>(),
            new TestOffset("a", "42"));

        IRawInboundEnvelope? result = null;
        await new ValidatorConsumerBehavior(_logger).HandleAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
            (context, _) =>
            {
                result = context.Envelope;
                return default;
            },
            CancellationToken.None);

        result.ShouldNotBeNull();
        expectedValidationMessage += " | EndpointName: topic1, BrokerMessageId: 42";
        _loggerSubstitute.Received(LogLevel.Warning, null, expectedValidationMessage, 1082);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenModeIsThrowException()
    {
        TestValidationMessage message = new() { Id = "1", String10 = "123456789abc", IntRange = 5, NumbersOnly = "123" };
        string expectedMessage =
            $"The message is not valid: {Environment.NewLine}- The field String10 must be a string with a maximum length of 10.";
        TestConsumerEndpoint endpoint = new TestConsumerEndpointConfiguration("topic1")
        {
            MessageValidationMode = MessageValidationMode.ThrowException
        }.GetDefaultEndpoint();
        InboundEnvelope envelope = new(
            message,
            null,
            null,
            endpoint,
            Substitute.For<IConsumer>(),
            new TestOffset());

        IRawInboundEnvelope? result = null;
        Func<Task> act = () => new ValidatorConsumerBehavior(_logger).HandleAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
            (context, _) =>
            {
                result = context.Envelope;
                return default;
            },
            CancellationToken.None).AsTask();

        Exception exception = await act.ShouldThrowAsync<MessageValidationException>();
        exception.Message.ShouldBe(expectedMessage);
        result.ShouldBeNull();
    }
}
