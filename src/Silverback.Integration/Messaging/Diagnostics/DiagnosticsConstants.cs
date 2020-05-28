// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Diagnostics
{
    internal static class DiagnosticsConstants
    {
        public static readonly string ActivityNameMessageConsuming = typeof(Consumer).FullName + "-ConsumeMessage";

        public static readonly string ActivityNameMessageProducing = typeof(Producer).FullName + "-ProduceMessage";
    }
}
