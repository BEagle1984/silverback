// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker;

namespace Silverback.Diagnostics
{
    internal class DiagnosticsConstants
    {
        public static readonly string ActivityNameMessageConsuming =  typeof(Consumer).FullName + "ConsumeMessage";

        public static readonly string ActivityNameMessageProducing = typeof(Producer).FullName + "ProduceMessage";

        public static readonly string TraceStateHeaderKey = "tracestate"; // According https://www.w3.org/TR/trace-context-1/#tracestate-header

        public const string TraceIdHeaderKey = "traceparent"; // According https://www.w3.org/TR/trace-context-1/#traceparent-header

        public const string TraceBaggageHeaderKey = "tracebaggage"; // Not part of the w3c standard
    }
}