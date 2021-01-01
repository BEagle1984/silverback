// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    internal static class BrokerOptionsBuilderInternalExtensions
    {
        public static LoggerCollection GetLoggerCollection(this IBrokerOptionsBuilder brokerOptionsBuilder) =>
            brokerOptionsBuilder.SilverbackBuilder.Services.GetSingletonServiceInstance<LoggerCollection>() ??
            throw new InvalidOperationException(
                "LoggerCollection not found, WithConnectionToMessageBroker has not been called.");
    }
}
