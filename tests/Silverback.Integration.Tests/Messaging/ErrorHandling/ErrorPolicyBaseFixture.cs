// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling;

public class ErrorPolicyBaseFixture
{
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "TestData")]
    [SuppressMessage("ReSharper", "CA2208", Justification = "Test")]

    public static IEnumerable<object[]> IncludedExceptions_TestData =>
    [
        [new ArgumentException(), true],
        [new ArgumentOutOfRangeException(), true],
        [new InvalidCastException(), true],
        [new FormatException(), false]
    ];

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "TestData")]
    [SuppressMessage("ReSharper", "CA2208", Justification = "Test")]
    public static IEnumerable<object[]> ExcludedException_TestData =>
    [
        [new ArgumentException(), false],
        [new ArgumentOutOfRangeException(), false],
        [new InvalidCastException(), false],
        [new FormatException(), true]
    ];

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "TestData")]
    [SuppressMessage("ReSharper", "CA2208", Justification = "Test")]
    public static IEnumerable<object[]> IncludedAndExcludedExceptions_TestData =>
    [
        [new ArgumentException(), true],
        [new ArgumentNullException(), true],
        [new ArgumentOutOfRangeException(), false],
        [new InvalidCastException(), false],
        [new FormatException(), true]
    ];

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "TestData")]
    [SuppressMessage("ReSharper", "CA2208", Justification = "Test")]
    public static IEnumerable<object[]> ApplyRule_TestData =>
    [
        [
            new InboundEnvelope(
                null,
                Stream.Null,
                [new MessageHeader(DefaultMessageHeaders.FailedAttempts, "3")],
                TestConsumerEndpoint.GetDefault(),
                Substitute.For<IConsumer>(),
                new TestOffset()),
            new ArgumentException(),
            true
        ],
        [
            new InboundEnvelope(
                null,
                Stream.Null,
                [new MessageHeader(DefaultMessageHeaders.FailedAttempts, "6")],
                TestConsumerEndpoint.GetDefault(),
                Substitute.For<IConsumer>(),
                new TestOffset()),
            new ArgumentException(),
            false
        ],
        [
            new InboundEnvelope(
                null,
                Stream.Null,
                [new MessageHeader(DefaultMessageHeaders.FailedAttempts, "3")],
                TestConsumerEndpoint.GetDefault(),
                Substitute.For<IConsumer>(),
                new TestOffset()),
            new ArgumentException("no"),
            false
        ]
    ];

    [Theory]
    [MemberData(nameof(IncludedExceptions_TestData))]
    public void CanHandle_ShouldEvaluateExceptionType_WhenIncludedExceptionsAreSpecified(Exception exception, bool mustApply)
    {
        TestErrorPolicy policy = new()
        {
            IncludedExceptions = [typeof(ArgumentException), typeof(InvalidCastException)]
        };
        IErrorPolicyImplementation policyImplementation = policy.Build(Substitute.For<IServiceProvider>());

        bool canHandle = policyImplementation.CanHandle(
            ConsumerPipelineContextHelper.CreateSubstitute(
                new InboundEnvelope(
                    null,
                    Stream.Null,
                    [new MessageHeader(DefaultMessageHeaders.FailedAttempts, "99")],
                    TestConsumerEndpoint.GetDefault(),
                    Substitute.For<IConsumer>(),
                    new TestOffset())),
            exception);

        canHandle.Should().Be(mustApply);
    }

    [Theory]
    [MemberData(nameof(ExcludedException_TestData))]
    public void CanHandle_ShouldEvaluateExceptionType_WhenExcludedExceptionsAreSpecified(Exception exception, bool mustApply)
    {
        TestErrorPolicy policy = new()
        {
            ExcludedExceptions = [typeof(ArgumentException), typeof(InvalidCastException)]
        };
        IErrorPolicyImplementation policyImplementation = policy.Build(Substitute.For<IServiceProvider>());

        bool canHandle = policyImplementation.CanHandle(
            ConsumerPipelineContextHelper.CreateSubstitute(
                new InboundEnvelope(
                    null,
                    Stream.Null,
                    [new MessageHeader(DefaultMessageHeaders.FailedAttempts, "99")],
                    TestConsumerEndpoint.GetDefault(),
                    Substitute.For<IConsumer>(),
                    new TestOffset())),
            exception);

        canHandle.Should().Be(mustApply);
    }

    [Theory]
    [MemberData(nameof(IncludedAndExcludedExceptions_TestData))]
    public void CanHandle_ShouldEvaluateExceptionType_WhenIncludedAndExcludedExceptionsAreSpecified(Exception exception, bool mustApply)
    {
        TestErrorPolicy policy = new()
        {
            IncludedExceptions = [typeof(ArgumentException), typeof(FormatException)],
            ExcludedExceptions = [typeof(ArgumentOutOfRangeException)]
        };
        IErrorPolicyImplementation policyImplementation = policy.Build(Substitute.For<IServiceProvider>());

        bool canHandle = policyImplementation.CanHandle(
            ConsumerPipelineContextHelper.CreateSubstitute(
                new InboundEnvelope(
                    null,
                    Stream.Null,
                    [new MessageHeader(DefaultMessageHeaders.FailedAttempts, "99")],
                    TestConsumerEndpoint.GetDefault(),
                    Substitute.For<IConsumer>(),
                    new TestOffset())),
            exception);

        canHandle.Should().Be(mustApply);
    }

    [Theory]
    [MemberData(nameof(ApplyRule_TestData))]
    public void CanHandle_ShouldEvaluateApplyRule(IInboundEnvelope envelope, Exception exception, bool mustApply)
    {
        TestErrorPolicy policy = new()
        {
            ApplyRule = (inboundEnvelope, ex) =>
                inboundEnvelope.Headers.GetValue<int>(DefaultMessageHeaders.FailedAttempts) <= 5 && ex.Message != "no"
        };
        IErrorPolicyImplementation policyImplementation = policy.Build(Substitute.For<IServiceProvider>());

        bool canHandle = policyImplementation.CanHandle(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope),
            exception);

        canHandle.Should().Be(mustApply);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(7, false)]
    public void CanHandle_ShouldEvaluateMaxFailedAttempts(int failedAttempts, bool expectedResult)
    {
        InboundEnvelope envelope = new(
            Stream.Null,
            [
                new MessageHeader(DefaultMessageHeaders.FailedAttempts, failedAttempts.ToString(CultureInfo.InvariantCulture))
            ],
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        TestErrorPolicy policy = new()
        {
            MaxFailedAttempts = 3
        };
        IErrorPolicyImplementation policyImplementation = policy.Build(Substitute.For<IServiceProvider>());

        bool canHandle = policyImplementation.CanHandle(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope),
            new InvalidOperationException());

        canHandle.Should().Be(expectedResult);
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldPublishMessage()
    {
        IPublisher? publisher = Substitute.For<IPublisher>();
        ServiceProvider serviceProvider = new ServiceCollection().AddScoped(_ => publisher)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        TestErrorPolicy policy = new()
        {
            MessageToPublishFactory = (_, exception) => new TestEventTwo { Content = exception.Message }
        };
        IErrorPolicyImplementation policyImplementation = policy.Build(serviceProvider);

        InboundEnvelope envelope = new(
            Stream.Null,
            [new MessageHeader(DefaultMessageHeaders.FailedAttempts, "3")],
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        await policyImplementation.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, serviceProvider),
            new TimeoutException("Exception message."));

        await publisher.Received().PublishAsync(Arg.Is<TestEventTwo>(message => message.Content == "Exception message."));
    }

    [Fact]
    public async Task HandleErrorAsync_ShouldNotPublishMessage_WhenFactoryReturnsNull()
    {
        IPublisher? publisher = Substitute.For<IPublisher>();
        ServiceProvider serviceProvider = new ServiceCollection().AddScoped(_ => publisher)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        TestErrorPolicy policy = new()
        {
            MessageToPublishFactory = (_, _) => null
        };
        IErrorPolicyImplementation policyImplementation = policy.Build(serviceProvider);

        InboundEnvelope envelope = new(
            Stream.Null,
            [
                new MessageHeader(DefaultMessageHeaders.FailedAttempts, "3")
            ],
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        await policyImplementation.HandleErrorAsync(
            ConsumerPipelineContextHelper.CreateSubstitute(envelope, serviceProvider),
            new ArgumentNullException());

        await publisher.DidNotReceive().PublishAsync(Arg.Any<object>());
    }
}
