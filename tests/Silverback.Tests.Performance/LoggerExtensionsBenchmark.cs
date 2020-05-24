// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;

namespace Silverback.Tests.Performance
{
    // Starting point
    // | Method |     Mean |     Error |    StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
    // |------- |---------:|----------:|----------:|-------:|------:|------:|----------:|
    // |    Log | 2.629 us | 0.0502 us | 0.0493 us | 0.6371 |     - |     - |   2.62 KB |
    [MemoryDiagnoser]
    public class LoggerExtensionsBenchmark
    {
        private readonly List<IRawBrokerEnvelope> _rawBrokerEnvelopes = new List<IRawBrokerEnvelope>
        {
            new RawInboundEnvelope(
                Array.Empty<byte>(),
                new List<MessageHeader>
                {
                    new MessageHeader("Test", "Test"),
                    new MessageHeader(DefaultMessageHeaders.FailedAttempts, 1),
                    new MessageHeader(DefaultMessageHeaders.MessageType, "Something.Xy"),
                    new MessageHeader(DefaultMessageHeaders.MessageId, "1234"),
                    new MessageHeader(DefaultMessageHeaders.BatchId, "1234"),
                    new MessageHeader(DefaultMessageHeaders.BatchSize, "11"),
                },
                new TestConsumerEndpoint("Test"),
                "Test",
                new TestOffset("abc", "1"))
        };

        [Benchmark]
        public void Log()
        {
            NullLogger.Instance.Log(
                LogLevel.Error,
                new EventId(1, "Something"),
                null,
                "This is my log message",
                _rawBrokerEnvelopes);
        }
    }
}
