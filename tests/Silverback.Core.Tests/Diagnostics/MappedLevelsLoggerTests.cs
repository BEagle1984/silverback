// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Diagnostics;

public class MappedLevelsLoggerTests
{
    private const string DiscardedResultMessage =
        "Discarding result of type TypeName because doesn't match expected return type ExpectedTypeName";

    private static readonly EventId MappedEventId = new(42);

    private static readonly EventId OtherEventId = new(43);

    [Theory]
    [InlineData(LogLevel.Debug, false)]
    [InlineData(LogLevel.Information, true)]
    [InlineData(LogLevel.Error, true)]
    public void IsEnabled_ShouldUseInnerLogger_WhenMappingIsEmpty(LogLevel logLevel, bool expected)
    {
        LoggerSubstitute<MappedLevelsLoggerTests> logger = new(LogLevel.Information);
        MappedLevelsLogger<MappedLevelsLoggerTests> mappedLevelsLogger = new([], logger);

        bool result = mappedLevelsLogger.IsEnabled(logLevel);

        result.ShouldBe(expected);
    }

    [Fact]
    public void IsEnabled_ShouldReturnTrue_WhenMappingIsNotEmpty()
    {
        LogLevelDictionary logLevels = new()
        {
            { MappedEventId, new LogLevelOverride((_, _, _) => LogLevel.Error) }
        };
        LoggerSubstitute<MappedLevelsLoggerTests> logger = new(LogLevel.Information);
        MappedLevelsLogger<MappedLevelsLoggerTests> mappedLevelsLogger = new(logLevels, logger);

        bool result = mappedLevelsLogger.IsEnabled(LogLevel.Debug);

        result.ShouldBeTrue();
    }

    [Theory]
    [InlineData(LogLevel.Debug, false)]
    [InlineData(LogLevel.Information, true)]
    public void IsEnabled_ShouldUseSpecifiedLevel_WhenEventIdIsNotMapped(LogLevel logLevel, bool expected)
    {
        LogLevelDictionary logLevels = new()
        {
            { MappedEventId, new LogLevelOverride((_, _, _) => LogLevel.Error) }
        };
        LoggerSubstitute<MappedLevelsLoggerTests> logger = new(LogLevel.Information);
        MappedLevelsLogger<MappedLevelsLoggerTests> mappedLevelsLogger = new(logLevels, logger);

        bool result = mappedLevelsLogger.IsEnabled(logLevel, OtherEventId);

        result.ShouldBe(expected);
    }

    [Fact]
    public void IsEnabled_ShouldReturnTrueWithoutInvokingMapping_WhenEventIdIsMapped()
    {
        bool isMappingInvoked = false;
        LogLevelDictionary logLevels = new()
        {
            {
                MappedEventId, new LogLevelOverride((_, originalLogLevel, _) =>
                {
                    isMappingInvoked = true;

                    return originalLogLevel;
                })
            }
        };
        LoggerSubstitute<MappedLevelsLoggerTests> logger = new(LogLevel.Information);
        MappedLevelsLogger<MappedLevelsLoggerTests> mappedLevelsLogger = new(logLevels, logger);

        bool result = mappedLevelsLogger.IsEnabled(LogLevel.Debug, MappedEventId);

        result.ShouldBeTrue();
        isMappingInvoked.ShouldBeFalse();
    }

    [Theory]
    [InlineData(LogLevel.Debug, LogLevel.Warning, true)]
    [InlineData(LogLevel.Warning, LogLevel.Debug, false)]
    public void IsEnabled_ShouldUseFixedLevel_WhenEventIdHasFixedLevel(LogLevel logLevel, LogLevel fixedLevel, bool expected)
    {
        LogLevelDictionary logLevels = new()
        {
            { MappedEventId, new LogLevelOverride(fixedLevel) }
        };
        LoggerSubstitute<MappedLevelsLoggerTests> logger = new(LogLevel.Information);
        MappedLevelsLogger<MappedLevelsLoggerTests> mappedLevelsLogger = new(logLevels, logger);

        bool result = mappedLevelsLogger.IsEnabled(logLevel, MappedEventId);

        result.ShouldBe(expected);
    }

    [Fact]
    public void Log_ShouldSkipLog_WhenMappingIsEmptyAndLevelIsDisabled()
    {
        LoggerSubstitute<MappedLevelsLoggerTests> logger = new(LogLevel.Information);
        MappedLevelsLogger<MappedLevelsLoggerTests> mappedLevelsLogger = new([], logger);
        SilverbackLogger<MappedLevelsLoggerTests> silverbackLogger = new(mappedLevelsLogger);

        silverbackLogger.LogSubscriberResultDiscarded("TypeName", "ExpectedTypeName");

        logger.DidNotReceive(LogLevel.Debug, null, DiscardedResultMessage);
    }

    [Fact]
    public void Log_ShouldUpliftLog_WhenDefaultLevelIsDisabled()
    {
        LogLevelDictionary logLevels = new()
        {
            { CoreLogEvents.SubscriberResultDiscarded.EventId, new LogLevelOverride((_, _, _) => LogLevel.Warning) }
        };
        LoggerSubstitute<MappedLevelsLoggerTests> logger = new(LogLevel.Information);
        MappedLevelsLogger<MappedLevelsLoggerTests> mappedLevelsLogger = new(logLevels, logger);
        SilverbackLogger<MappedLevelsLoggerTests> silverbackLogger = new(mappedLevelsLogger);

        silverbackLogger.LogSubscriberResultDiscarded("TypeName", "ExpectedTypeName");

        logger.Received(LogLevel.Warning, null, DiscardedResultMessage);
    }

    [Fact]
    public void Log_ShouldNotFormatMessage_WhenEventIdIsNotMapped()
    {
        LogLevelDictionary logLevels = new()
        {
            { MappedEventId, new LogLevelOverride((_, _, _) => LogLevel.Error) }
        };
        ILogger<MappedLevelsLoggerTests> logger = Substitute.For<ILogger<MappedLevelsLoggerTests>>();
        MappedLevelsLogger<MappedLevelsLoggerTests> mappedLevelsLogger = new(logLevels, logger);
        int formatCalls = 0;

        mappedLevelsLogger.Log(
            LogLevel.Information,
            OtherEventId,
            "state",
            null,
            (state, _) =>
            {
                formatCalls++;
                return state;
            });

        formatCalls.ShouldBe(0);
    }

    [Fact]
    public void Log_ShouldNotFormatMessage_WhenMappingDoesNotUseIt()
    {
        LogLevelDictionary logLevels = new()
        {
            { MappedEventId, new LogLevelOverride((_, _, _) => LogLevel.Error) }
        };
        ILogger<MappedLevelsLoggerTests> logger = Substitute.For<ILogger<MappedLevelsLoggerTests>>();
        MappedLevelsLogger<MappedLevelsLoggerTests> mappedLevelsLogger = new(logLevels, logger);
        int formatCalls = 0;

        mappedLevelsLogger.Log(
            LogLevel.Information,
            MappedEventId,
            "state",
            null,
            (state, _) =>
            {
                formatCalls++;
                return state;
            });

        formatCalls.ShouldBe(0);
    }

    [Fact]
    public void Log_ShouldPassFormattedMessageAndExceptionToMapping()
    {
        InvalidOperationException exception = new();
        Exception? receivedException = null;
        string? receivedMessage = null;
        LogLevelDictionary logLevels = new()
        {
            {
                MappedEventId, new LogLevelOverride((mappedException, originalLogLevel, message) =>
                {
                    receivedException = mappedException;
                    receivedMessage = message.Value;

                    return originalLogLevel;
                })
            }
        };
        LoggerSubstitute<MappedLevelsLoggerTests> logger = new(LogLevel.Information);
        MappedLevelsLogger<MappedLevelsLoggerTests> mappedLevelsLogger = new(logLevels, logger);

        mappedLevelsLogger.Log(LogLevel.Error, MappedEventId, "state", exception, (state, _) => $"formatted {state}");

        receivedException.ShouldBeSameAs(exception);
        receivedMessage.ShouldBe("formatted state");
    }
}
