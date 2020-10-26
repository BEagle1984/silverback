// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Tests.Performance.TestTypes;
using Silverback.Tests.Types;

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
    //
    // After introducing additional log data
    //
    // |                    Method |         Mean |      Error |     StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
    // |-------------------------- |-------------:|-----------:|-----------:|-------:|------:|------:|----------:|
    // |    LogErrorSingleEnvelope | 1,411.879 ns | 18.1310 ns | 16.0726 ns | 0.5188 |     - |     - |    1632 B |
    // | LogErrorMultipleEnvelopes |   973.243 ns | 19.2753 ns | 48.0021 ns | 0.2460 |     - |     - |     776 B |
    // |           LogInfoDisabled |     8.390 ns |  0.2018 ns |  0.2159 ns |      - |     - |     - |         - |
    // |-------------------------- |-------------:|-----------:|-----------:|-------:|------:|------:|----------:|
    // |    LogErrorSingleEnvelope | 1,273.400 ns | 27.9831 ns | 79.8374 ns | 0.3643 |     - |     - |    1144 B |
    // | LogErrorMultipleEnvelopes |   917.785 ns | 18.1426 ns | 26.0196 ns | 0.2470 |     - |     - |     776 B |
    // |           LogInfoDisabled |     9.259 ns |  0.2249 ns |  0.5257 ns |      - |     - |     - |         - |
    // |-------------------------- |-------------:|-----------:|-----------:|-------:|------:|------:|----------:|
    // |    LogErrorSingleEnvelope | 1,395.579 ns | 27.8423 ns | 81.6567 ns | 0.4711 |     - |     - |    1480 B |
    // | LogErrorMultipleEnvelopes |   902.685 ns | 17.6931 ns | 18.9314 ns | 0.2470 |     - |     - |     776 B |
    // |           LogInfoDisabled |     8.275 ns |  0.2005 ns |  0.2677 ns |      - |     - |     - |         - |
    // |-------------------------- |-------------:|-----------:|-----------:|-------:|------:|------:|----------:|
    // |    LogErrorSingleEnvelope | 1,269.185 ns | 25.2160 ns | 41.4305 ns | 0.3738 |     - |     - |    1176 B |
    // | LogErrorMultipleEnvelopes |   946.940 ns | 18.7865 ns | 51.7435 ns | 0.2470 |     - |     - |     776 B |
    // |           LogInfoDisabled |     8.033 ns |  0.1791 ns |  0.1675 ns |      - |     - |     - |         - |
    // |-------------------------- |-------------:|-----------:|-----------:|-------:|------:|------:|----------:|
    // |    LogErrorSingleEnvelope | 1,321.796 ns | 26.2344 ns | 41.6106 ns | 0.3605 |     - |     - |    1136 B |
    // | LogErrorMultipleEnvelopes |   906.357 ns | 18.0801 ns | 24.1365 ns | 0.2470 |     - |     - |     776 B |
    // |           LogInfoDisabled |     7.857 ns |  0.1349 ns |  0.1196 ns |      - |     - |     - |         - |
    [MemoryDiagnoser]
    public class SilverbackIntegrationLoggerBenchmark
    {
        private readonly ConsumerPipelineContext _singleMessageContext;

        private readonly ConsumerPipelineContext _sequenceContext;

        private readonly ISilverbackIntegrationLogger _integrationLogger;

        public SilverbackIntegrationLoggerBenchmark()
        {
            _integrationLogger = new SilverbackIntegrationLogger(
                new FakeLogger(),
                new LogTemplates().ConfigureAdditionalData<TestConsumerEndpoint>("offset"));

            _singleMessageContext = ConsumerPipelineContextHelper.CreateSubstitute(
                new RawInboundEnvelope(
                    Array.Empty<byte>(),
                    new MessageHeaderCollection
                    {
                        new MessageHeader("Test", "Test"),
                        new MessageHeader(DefaultMessageHeaders.FailedAttempts, 1),
                        new MessageHeader(DefaultMessageHeaders.MessageType, "Something.Xy"),
                        new MessageHeader(DefaultMessageHeaders.MessageId, "1234"),
                    },
                    new TestConsumerEndpoint("Test"),
                    "Test",
                    new TestOffset("abc", "1"),
                    new Dictionary<string, string>
                    {
                        ["offset"] = "1@42"
                    }));

            _sequenceContext = ConsumerPipelineContextHelper.CreateSubstitute(
                new RawInboundEnvelope(
                    Array.Empty<byte>(),
                    new MessageHeaderCollection
                    {
                        new MessageHeader("Test", "Test"),
                        new MessageHeader(DefaultMessageHeaders.FailedAttempts, 1),
                        new MessageHeader(DefaultMessageHeaders.MessageType, "Something.Xy"),
                        new MessageHeader(DefaultMessageHeaders.MessageId, "5678"),
                        new MessageHeader(DefaultMessageHeaders.BatchId, "1234"),
                        new MessageHeader(DefaultMessageHeaders.BatchSize, "11"),
                    },
                    new TestConsumerEndpoint("Test"),
                    "Test",
                    new TestOffset("abc", "2"),
                    new Dictionary<string, string>
                    {
                        ["offset"] = "1@43"
                    }));
            var sequence = new BatchSequence("123", _sequenceContext);
            sequence.AddAsync(_sequenceContext.Envelope, null, false);
            _sequenceContext.SetSequence(sequence, true);
        }

        [Benchmark]
        public void LogErrorSingleMessage()
        {
            _integrationLogger.LogWithMessageInfo(
                LogLevel.Error,
                new EventId(1, "Something"),
                null,
                "This is my log message",
                _singleMessageContext);
        }

        [Benchmark]
        public void LogErrorSequence()
        {
            _integrationLogger.LogWithMessageInfo(
                LogLevel.Error,
                new EventId(1, "Something"),
                null,
                "This is my log message",
                _sequenceContext);
        }

        [Benchmark]
        public void LogInfoDisabled()
        {
            _integrationLogger.LogWithMessageInfo(
                LogLevel.Information,
                new EventId(1, "Something"),
                null,
                "This is my log message",
                _singleMessageContext);
        }
    }
}
