// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.Extensions;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker.Callbacks;

public class KafkaEventsBinderTests
{
    private readonly LoggerSubstitute<KafkaEventsBinderTests> _loggerSubstitute = new(LogLevel.Trace);

    private readonly ISilverbackLogger _logger;

    private readonly IBrokerClientCallbacksInvoker _callbacksInvoker = Substitute.For<IBrokerClientCallbacksInvoker>();

    public KafkaEventsBinderTests()
    {
        _logger = new SilverbackLogger<KafkaEventsBinderTests>(new MappedLevelsLogger<KafkaEventsBinderTests>([], _loggerSubstitute));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ConsumerLogHandler_ShouldLogOnlyIfNoCallbackHandledTheLog(bool firstHandled, bool secondHandled)
    {
        IKafkaConsumerLogCallback firstCallback = Substitute.For<IKafkaConsumerLogCallback>();
        IKafkaConsumerLogCallback secondCallback = Substitute.For<IKafkaConsumerLogCallback>();
        firstCallback.OnConsumerLog(Arg.Any<LogMessage>(), Arg.Any<IKafkaConsumer>()).Returns(firstHandled);
        secondCallback.OnConsumerLog(Arg.Any<LogMessage>(), Arg.Any<IKafkaConsumer>()).Returns(secondHandled);
        SetupCallbacks(firstCallback, secondCallback);

        IConfluentConsumerBuilder builder = GetConsumerBuilder();
        Action<IConsumer<byte[]?, byte[]?>, LogMessage>? logHandler = null;
        builder.When(consumerBuilder => consumerBuilder.SetLogHandler(Arg.Any<Action<IConsumer<byte[]?, byte[]?>, LogMessage>>()))
            .Do(call => logHandler = call.Arg<Action<IConsumer<byte[]?, byte[]?>, LogMessage>>());

        builder.SetEventsHandlers(GetConsumerWrapper(), new KafkaConsumerConfiguration(), _callbacksInvoker, _logger);

        logHandler.ShouldNotBeNull().Invoke(Substitute.For<IConsumer<byte[]?, byte[]?>>(), GetLogMessage());

        firstCallback.ReceivedWithAnyArgs(1).OnConsumerLog(null!, null!);
        secondCallback.ReceivedWithAnyArgs(1).OnConsumerLog(null!, null!);
        AssertLogged(firstHandled || secondHandled, KafkaLogEvents.ConfluentConsumerLogWarning);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ConsumerErrorHandler_ShouldLogOnlyIfNoCallbackHandledTheError(bool firstHandled, bool secondHandled)
    {
        IKafkaConsumerErrorCallback firstCallback = Substitute.For<IKafkaConsumerErrorCallback>();
        IKafkaConsumerErrorCallback secondCallback = Substitute.For<IKafkaConsumerErrorCallback>();
        firstCallback.OnConsumerError(Arg.Any<Error>(), Arg.Any<IKafkaConsumer>()).Returns(firstHandled);
        secondCallback.OnConsumerError(Arg.Any<Error>(), Arg.Any<IKafkaConsumer>()).Returns(secondHandled);
        SetupCallbacks(firstCallback, secondCallback);

        IConfluentConsumerBuilder builder = GetConsumerBuilder();
        Action<IConsumer<byte[]?, byte[]?>, Error>? errorHandler = null;
        builder.When(consumerBuilder => consumerBuilder.SetErrorHandler(Arg.Any<Action<IConsumer<byte[]?, byte[]?>, Error>>()))
            .Do(call => errorHandler = call.Arg<Action<IConsumer<byte[]?, byte[]?>, Error>>());

        builder.SetEventsHandlers(GetConsumerWrapper(), new KafkaConsumerConfiguration(), _callbacksInvoker, _logger);

        errorHandler.ShouldNotBeNull().Invoke(Substitute.For<IConsumer<byte[]?, byte[]?>>(), new Error(ErrorCode.Local_Transport));

        firstCallback.ReceivedWithAnyArgs(1).OnConsumerError(null!, null!);
        secondCallback.ReceivedWithAnyArgs(1).OnConsumerError(null!, null!);
        AssertLogged(firstHandled || secondHandled, KafkaLogEvents.ConfluentConsumerError);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ProducerLogHandler_ShouldLogOnlyIfNoCallbackHandledTheLog(bool firstHandled, bool secondHandled)
    {
        IKafkaProducerLogCallback firstCallback = Substitute.For<IKafkaProducerLogCallback>();
        IKafkaProducerLogCallback secondCallback = Substitute.For<IKafkaProducerLogCallback>();
        firstCallback.OnProducerLog(Arg.Any<LogMessage>(), Arg.Any<IConfluentProducerWrapper>()).Returns(firstHandled);
        secondCallback.OnProducerLog(Arg.Any<LogMessage>(), Arg.Any<IConfluentProducerWrapper>()).Returns(secondHandled);
        SetupCallbacks(firstCallback, secondCallback);

        IConfluentProducerBuilder builder = Substitute.For<IConfluentProducerBuilder>();
        builder.ReturnsForAll(builder);
        Action<IProducer<byte[]?, byte[]?>, LogMessage>? logHandler = null;
        builder.When(producerBuilder => producerBuilder.SetLogHandler(Arg.Any<Action<IProducer<byte[]?, byte[]?>, LogMessage>>()))
            .Do(call => logHandler = call.Arg<Action<IProducer<byte[]?, byte[]?>, LogMessage>>());

        builder.SetEventsHandlers(Substitute.For<IConfluentProducerWrapper>(), _callbacksInvoker, _logger);

        logHandler.ShouldNotBeNull().Invoke(Substitute.For<IProducer<byte[]?, byte[]?>>(), GetLogMessage());

        firstCallback.ReceivedWithAnyArgs(1).OnProducerLog(null!, null!);
        secondCallback.ReceivedWithAnyArgs(1).OnProducerLog(null!, null!);
        AssertLogged(firstHandled || secondHandled, KafkaLogEvents.ConfluentProducerLogWarning);
    }

    private static IConfluentConsumerBuilder GetConsumerBuilder()
    {
        IConfluentConsumerBuilder builder = Substitute.For<IConfluentConsumerBuilder>();
        builder.ReturnsForAll(builder);
        return builder;
    }

    private static IConfluentConsumerWrapper GetConsumerWrapper()
    {
        IConfluentConsumerWrapper consumerWrapper = Substitute.For<IConfluentConsumerWrapper>();
        consumerWrapper.Status.Returns(ClientStatus.Initialized);
        consumerWrapper.Initialized.Returns(new AsyncEvent<BrokerClient>());
        consumerWrapper.Disconnecting.Returns(new AsyncEvent<BrokerClient>());

        KafkaConsumer consumer = new(
            "consumer1",
            consumerWrapper,
            new KafkaConsumerConfiguration(),
            Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>(),
            Substitute.For<IBrokerClientCallbacksInvoker>(),
            Substitute.For<IKafkaOffsetStoreFactory>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<ISilverbackLogger<KafkaConsumer>>());
        consumerWrapper.Consumer.Returns(consumer);

        return consumerWrapper;
    }

    private static LogMessage GetLogMessage() => new("rdkafka", SyslogLevel.Warning, "TEST", "test message");

    private void SetupCallbacks<TCallback>(params TCallback[] callbacks) =>
        _callbacksInvoker.When(invoker => invoker.Invoke(Arg.Any<Action<TCallback>>())).Do(call =>
        {
            foreach (TCallback callback in callbacks)
            {
                call.Arg<Action<TCallback>>().Invoke(callback);
            }
        });

    private void AssertLogged(bool handled, LogEvent logEvent)
    {
        if (handled)
            _loggerSubstitute.DidNotReceive(logEvent.Level, null, eventId: logEvent.EventId.Id);
        else
            _loggerSubstitute.Received(logEvent.Level, null, eventId: logEvent.EventId.Id);
    }
}
