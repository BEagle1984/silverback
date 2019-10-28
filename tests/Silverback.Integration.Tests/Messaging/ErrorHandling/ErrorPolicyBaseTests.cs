// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

// ReSharper disable InconsistentNaming

namespace Silverback.Tests.Integration.Messaging.ErrorHandling
{
    public class ErrorPolicyBaseTests
    {
        [Theory, MemberData(nameof(ApplyTo_TestData))]
        public void ApplyTo_Exception_CanHandleReturnsExpectedResult(Exception exception, bool mustApply)
        {
            var policy = new TestErrorPolicy()
                .ApplyTo<ArgumentException>()
                .ApplyTo<InvalidCastException>();

            var canHandle = policy.CanHandle(
                new InboundMessage(
                    new byte[1],
                    new[] { new MessageHeader(MessageHeader.FailedAttemptsKey, "99") },
                    null, TestEndpoint.GetDefault(), true),
                exception);

            canHandle.Should().Be(mustApply);
        }

        public static IEnumerable<object[]> ApplyTo_TestData =>
            new[]
            {
                new object[] { new ArgumentException(), true },
                new object[] { new ArgumentOutOfRangeException(), true },
                new object[] { new InvalidCastException(), true },
                new object[] { new FormatException(), false }
            };

        [Theory, MemberData(nameof(Exclude_TestData))]
        public void Exclude_Exception_CanHandleReturnsExpectedResult(Exception exception, bool mustApply)
        {
            var policy = (TestErrorPolicy)new TestErrorPolicy()
                .Exclude<ArgumentException>()
                .Exclude<InvalidCastException>();

            var canHandle = policy.CanHandle(
                new InboundMessage(
                    new byte[1],
                    new[] { new MessageHeader(MessageHeader.FailedAttemptsKey, "99") },
                    null, TestEndpoint.GetDefault(), true),
                exception);

            canHandle.Should().Be(mustApply);
        }

        public static IEnumerable<object[]> Exclude_TestData =>
            new[]
            {
                new object[] { new ArgumentException(), false },
                new object[] { new ArgumentOutOfRangeException(), false },
                new object[] { new InvalidCastException(), false },
                new object[] { new FormatException(), true }
            };

        [Theory, MemberData(nameof(ApplyToAndExclude_TestData))]
        public void ApplyToAndExclude_Exception_CanHandleReturnsExpectedResult(Exception exception, bool mustApply)
        {
            var policy = (TestErrorPolicy) new TestErrorPolicy()
                .ApplyTo<ArgumentException>()
                .Exclude<ArgumentOutOfRangeException>()
                .ApplyTo<FormatException>();

            var canHandle = policy.CanHandle(
                new InboundMessage(
                    new byte[1],
                    new[] { new MessageHeader(MessageHeader.FailedAttemptsKey, "99") },
                    null, TestEndpoint.GetDefault(), true),
                exception);

            canHandle.Should().Be(mustApply);
        }

        public static IEnumerable<object[]> ApplyToAndExclude_TestData =>
            new[]
            {
                new object[] { new ArgumentException(), true },
                new object[] { new ArgumentNullException(), true },
                new object[] { new ArgumentOutOfRangeException(), false },
                new object[] { new InvalidCastException(), false },
                new object[] { new FormatException(), true }
            };

        [Theory, MemberData(nameof(ApplyWhen_TestData))]
        public void ApplyWhen_Exception_CanHandleReturnsExpectedResult(IInboundMessage message, Exception exception, bool mustApply)
        {
            var policy = (TestErrorPolicy)new TestErrorPolicy()
                .ApplyWhen((msg, ex) => msg.Headers.GetValue<int>(MessageHeader.FailedAttemptsKey) <= 5 && ex.Message != "no");

            var canHandle = policy.CanHandle(message, exception);

            canHandle.Should().Be(mustApply);
        }

        public static IEnumerable<object[]> ApplyWhen_TestData =>
            new[]
            {
                new object[]
                {
                    new InboundMessage(
                        new byte[1],
                        new[] { new MessageHeader(MessageHeader.FailedAttemptsKey, "3") },
                        null, TestEndpoint.GetDefault(), true),
                    new ArgumentException(),
                    true
                },
                new object[]
                {
                    new InboundMessage(
                        new byte[1],
                        new[] { new MessageHeader(MessageHeader.FailedAttemptsKey, "6") },
                        null, TestEndpoint.GetDefault(), true),
                    new ArgumentException(),
                    false
                },
                new object[]
                {
                    new InboundMessage(
                        new byte[1],
                        new[] { new MessageHeader(MessageHeader.FailedAttemptsKey, "3") },
                        null, TestEndpoint.GetDefault(), true),
                    new ArgumentException("no"),
                    false
                }
            };

        [Fact]
        public void Publish_Exception_MessagePublished()
        {
            var publisher = Substitute.For<IPublisher>();
            var serviceProvider = new ServiceCollection().AddScoped(_ => publisher)
                .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
            var policy =
                (TestErrorPolicy) new TestErrorPolicy(serviceProvider).Publish(msg => new TestEventTwo
                    { Content = "aaa" });
            var message = new InboundMessage(
                new byte[1],
                new[] { new MessageHeader(MessageHeader.FailedAttemptsKey, "3") },
                null, TestEndpoint.GetDefault(), true);


            policy.HandleError(new[] { message }, new ArgumentNullException());

            publisher.Received().Publish(Arg.Any<TestEventTwo>());
        }

        // TODO: Test with multiple messages (batch)
    }
}