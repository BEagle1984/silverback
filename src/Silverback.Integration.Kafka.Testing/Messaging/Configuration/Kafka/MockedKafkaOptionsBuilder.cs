// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka;

internal sealed class MockedKafkaOptionsBuilder : IMockedKafkaOptionsBuilder
{
    public MockedKafkaOptionsBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }

    public IMockedKafkaOptions MockedKafkaOptions =>
        Services.GetSingletonServiceInstance<IMockedKafkaOptions>() ??
        throw new InvalidOperationException("IMockedKafkaOptions not found, AddMockedKafka has not been called.");

    public IMockedKafkaOptionsBuilder WithDefaultPartitionsCount(int partitionsCount)
    {
        MockedKafkaOptions.DefaultPartitionsCount = partitionsCount;
        return this;
    }

    public IMockedKafkaOptionsBuilder WithPartitionsCount(string topicName, int partitionsCount)
    {
        MockedKafkaOptions.TopicPartitionsCount[topicName] = partitionsCount;
        return this;
    }

    public IMockedKafkaOptionsBuilder OverrideAutoCommitIntervalMs(int? intervalMs)
    {
        MockedKafkaOptions.OverriddenAutoCommitIntervalMs = intervalMs;
        return this;
    }

    public IMockedKafkaOptionsBuilder DelayPartitionsAssignment(TimeSpan delay)
    {
        MockedKafkaOptions.PartitionsAssignmentDelay = delay;
        return this;
    }
}
