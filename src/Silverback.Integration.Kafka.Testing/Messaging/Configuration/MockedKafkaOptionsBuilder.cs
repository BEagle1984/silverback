// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    internal class MockedKafkaOptionsBuilder : IMockedKafkaOptionsBuilder
    {
        public MockedKafkaOptionsBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }

        public IMockedKafkaOptions BusOptions =>
            Services.GetSingletonServiceInstance<IMockedKafkaOptions>() ??
            throw new InvalidOperationException("IMockedKafkaOptions not found, AddMockedKafka has not been called.");

        public IMockedKafkaOptionsBuilder WithDefaultPartitionsCount(int partitionsCount)
        {
            BusOptions.DefaultPartitionsCount = partitionsCount;
            return this;
        }
    }
}
