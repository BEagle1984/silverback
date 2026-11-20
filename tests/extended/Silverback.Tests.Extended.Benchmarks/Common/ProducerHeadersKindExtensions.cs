// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;

namespace Silverback.Tests.Extended.Benchmarks.Common;

public static class ProducerHeadersKindExtensions
{
    private static readonly Func<BenchmarkMessage?, object?> IdProvider = message => message?.Id;

    private static readonly Func<BenchmarkMessage?, object?> NameProvider = message => message?.Name;

    private static readonly Func<BenchmarkMessage?, object?> LengthProvider = message => message?.Payload.Length;

    public static TBuilder AddHeaders<TConfiguration, TEndpoint, TBuilder>(
        this ProducerEndpointConfigurationBuilder<BenchmarkMessage, TConfiguration, TEndpoint, TBuilder> builder,
        ProducerHeadersKind kind)
        where TConfiguration : ProducerEndpointConfiguration<TEndpoint>
        where TEndpoint : ProducerEndpoint
        where TBuilder : ProducerEndpointConfigurationBuilder<BenchmarkMessage, TConfiguration, TEndpoint, TBuilder>
    {
        switch (kind)
        {
            case ProducerHeadersKind.FiveStatic:
                AddStaticHeaders(builder, 5);
                break;
            case ProducerHeadersKind.TenStatic:
                AddStaticHeaders(builder, 10);
                break;
            case ProducerHeadersKind.FiveValueProvider:
                builder
                    .AddHeader("benchmark-header-1", IdProvider)
                    .AddHeader("benchmark-header-2", NameProvider)
                    .AddHeader("benchmark-header-3", LengthProvider)
                    .AddHeader("benchmark-header-4", IdProvider)
                    .AddHeader("benchmark-header-5", NameProvider);
                break;
        }

        return (TBuilder)builder;
    }

    private static void AddStaticHeaders<TConfiguration, TEndpoint, TBuilder>(
        ProducerEndpointConfigurationBuilder<BenchmarkMessage, TConfiguration, TEndpoint, TBuilder> builder,
        int count)
        where TConfiguration : ProducerEndpointConfiguration<TEndpoint>
        where TEndpoint : ProducerEndpoint
        where TBuilder : ProducerEndpointConfigurationBuilder<BenchmarkMessage, TConfiguration, TEndpoint, TBuilder>
    {
        for (int i = 1; i <= count; i++)
        {
            builder.AddHeader($"benchmark-header-{i}", $"value-{i}");
        }
    }
}
