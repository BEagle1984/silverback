// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Silverback.Tests
{
    public static class LoggerSubstituteExtensions
    {
        public static void ReceivedLog(this ILogger logger, LogLevel logLevel, Type? exceptionType) =>
            logger.Received().Log(
                logLevel,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                exceptionType != null ? Arg.Is<Exception>(ex => ex.GetType() == exceptionType) : null,
                Arg.Any<Func<object, Exception, string>>());
    }
}
