// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Silverback.Messaging.KafkaEvents
{
    internal static class ConfluentSysLogLevelExtensions
    {
        public static LogLevel ToLogLevel(this SyslogLevel syslogLevel) =>
            syslogLevel switch
            {
                SyslogLevel.Emergency => LogLevel.Critical,
                SyslogLevel.Alert => LogLevel.Critical,
                SyslogLevel.Critical => LogLevel.Critical,
                SyslogLevel.Error => LogLevel.Error,
                SyslogLevel.Warning => LogLevel.Warning,
                SyslogLevel.Notice => LogLevel.Information,
                SyslogLevel.Info => LogLevel.Information,
                SyslogLevel.Debug => LogLevel.Debug,
                _ => throw new ArgumentOutOfRangeException(nameof(syslogLevel), syslogLevel, null)
            };
    }
}
