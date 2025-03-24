// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Broker;

internal static class BrokerConstants
{
    public static readonly TimeSpan ConsumerReconnectRetryDelay = TimeSpan.FromSeconds(5);
}
