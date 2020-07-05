// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Performance.TestTypes;

namespace Silverback.Tests.Performance
{
    // Starting point
    //
    // |                    Method |         Mean |      Error |     StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
    // |-------------------------- |-------------:|-----------:|-----------:|-------:|------:|------:|----------:|
    // |    LogErrorSingleEnvelope | 2,741.769 ns | 30.9451 ns | 24.1599 ns | 0.8392 |     - |     - |    2640 B |
    // | LogErrorMultipleEnvelopes | 2,756.648 ns | 44.9049 ns | 37.4976 ns | 0.8392 |     - |     - |    2640 B |
    // |           LogInfoDisabled |     9.061 ns |  0.2153 ns |  0.2799 ns |      - |     - |     - |         - |
    //
    // After first optimizations (constant message attributes template)
    //
    // |                    Method |         Mean |      Error |     StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
    // |-------------------------- |-------------:|-----------:|-----------:|-------:|------:|------:|----------:|
    // |    LogErrorSingleEnvelope | 1,231.628 ns | 24.0593 ns | 40.8546 ns | 0.3681 |     - |     - |    1160 B |
    // | LogErrorMultipleEnvelopes | 1,242.316 ns | 24.1488 ns | 21.4073 ns | 0.3681 |     - |     - |    1160 B |
    // |           LogInfoDisabled |     7.368 ns |  0.1763 ns |  0.1377 ns |      - |     - |     - |         - |
    [MemoryDiagnoser]
    public class LoggerExtensionsBenchmark
    {
        private static readonly List<IRawBrokerEnvelope> SingleEnvelope = new List<IRawBrokerEnvelope>
        {
            new RawInboundEnvelope(
                Array.Empty<byte>(),
                new MessageHeaderCollection
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

        private static readonly List<IRawBrokerEnvelope> Envelopes = new List<IRawBrokerEnvelope>
        {
            new RawInboundEnvelope(
                Array.Empty<byte>(),
                new MessageHeaderCollection
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

        private readonly ILogger _logger = new FakeLogger();

        [Benchmark]
        public void LogErrorSingleEnvelope()
        {
            _logger.LogWithMessageInfo(
                LogLevel.Error,
                new EventId(1, "Something"),
                null,
                "This is my log message",
                SingleEnvelope);
        }

        [Benchmark]
        public void LogErrorMultipleEnvelopes()
        {
            _logger.LogWithMessageInfo(
                LogLevel.Error,
                new EventId(1, "Something"),
                null,
                "This is my log message",
                Envelopes);
        }

        [Benchmark]
        public void LogInfoDisabled()
        {
            _logger.LogWithMessageInfo(
                LogLevel.Information,
                new EventId(1, "Something"),
                null,
                "This is my log message",
                SingleEnvelope);
        }
    }
}
