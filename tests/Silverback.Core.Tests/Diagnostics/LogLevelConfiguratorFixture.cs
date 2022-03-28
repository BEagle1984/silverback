// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Xunit;

namespace Silverback.Tests.Core.Diagnostics;

public class LogLevelConfiguratorFixture
{
    [Fact]
    public void SetLogLevel_ShouldAddFixedLogLevel()
    {
        LogLevelConfigurator configurator = new();
        EventId eventId = new(42);

        configurator.SetLogLevel(eventId, LogLevel.Warning);

        configurator.LogLevelDictionary[eventId]
            .Invoke(null, LogLevel.None, new Lazy<string>())
            .Should().Be(LogLevel.Warning);
    }

    [Fact]
    public void SetLogLevel_ShouldAddLogLevelFunction()
    {
        LogLevelConfigurator configurator = new();
        EventId eventId = new(42);

        configurator.SetLogLevel(
            eventId,
            (exception, _) => exception is InvalidOperationException ? LogLevel.Critical : LogLevel.Error);

        configurator.LogLevelDictionary[eventId]
            .Invoke(new InvalidOperationException(), LogLevel.None, new Lazy<string>())
            .Should().Be(LogLevel.Critical);
        configurator.LogLevelDictionary[eventId]
            .Invoke(new ArithmeticException(), LogLevel.None, new Lazy<string>())
            .Should().Be(LogLevel.Error);
    }

    [Fact]
    public void SetLogLevel_ShouldAddLogLevelFunctionWithMessage()
    {
        LogLevelConfigurator configurator = new();
        EventId eventId = new(42);

        configurator.SetLogLevel(
            eventId,
            (exception, _, _) => exception is InvalidOperationException ? LogLevel.Critical : LogLevel.Error);

        configurator.LogLevelDictionary[eventId]
            .Invoke(new InvalidOperationException(), LogLevel.None, new Lazy<string>())
            .Should().Be(LogLevel.Critical);
        configurator.LogLevelDictionary[eventId]
            .Invoke(new ArithmeticException(), LogLevel.None, new Lazy<string>())
            .Should().Be(LogLevel.Error);
    }
}
