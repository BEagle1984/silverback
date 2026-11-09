// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Shouldly;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Util;

public class JsonSerializerOptionsComparerTests
{
    [Fact]
    public void AreEqual_ShouldReturnTrue_WhenBothNull()
    {
        bool result = JsonSerializerOptionsComparer.AreEqual(null, null);

        result.ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_ShouldReturnTrue_WhenComparingNullWithDefaultOptions()
    {
        bool result = JsonSerializerOptionsComparer.AreEqual(null, new JsonSerializerOptions());

        result.ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_ShouldReturnTrue_WhenSameSettings()
    {
        JsonSerializerOptions options1 = new()
        {
            AllowTrailingCommas = true,
            MaxDepth = 42,
            NewLine = "\n"
        };
        JsonSerializerOptions options2 = new()
        {
            AllowTrailingCommas = true,
            MaxDepth = 42,
            NewLine = "\n"
        };

        bool result = JsonSerializerOptionsComparer.AreEqual(options1, options2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_ShouldReturnFalse_WhenDifferentSettings()
    {
        JsonSerializerOptions options1 = new() { MaxDepth = 42 };
        JsonSerializerOptions options2 = new() { MaxDepth = 13 };

        bool result = JsonSerializerOptionsComparer.AreEqual(options1, options2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void AreEqual_ShouldReturnFalse_WhenComparingNullWithCustomOptions()
    {
        bool result = JsonSerializerOptionsComparer.AreEqual(null, new JsonSerializerOptions { MaxDepth = 42 });

        result.ShouldBeFalse();
    }

    [Fact]
    public void AreEqual_ShouldReturnTrue_WhenOptionsHaveBeenUsed()
    {
        JsonSerializerOptions options1 = new() { MaxDepth = 42 };
        JsonSerializerOptions options2 = new() { MaxDepth = 42 };

        JsonSerializer.Serialize(42, options1);

        bool result = JsonSerializerOptionsComparer.AreEqual(options1, options2);

        options1.IsReadOnly.ShouldBeTrue();
        result.ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_ShouldNotAlterOptions()
    {
        JsonSerializerOptions options1 = new();
        JsonSerializerOptions options2 = new() { MaxDepth = 42 };

        JsonSerializerOptionsComparer.AreEqual(options1, options2);

        options1.IsReadOnly.ShouldBeFalse();
        options1.TypeInfoResolver.ShouldBeNull();
        options2.IsReadOnly.ShouldBeFalse();
        options2.TypeInfoResolver.ShouldBeNull();
    }

    [Fact]
    public void AreEqual_ShouldReturnTrue_WhenDefaultResolverIsExplicitlySet()
    {
        JsonSerializerOptions options1 = new() { TypeInfoResolver = JsonSerializerOptions.Default.TypeInfoResolver };
        JsonSerializerOptions options2 = new();

        bool result = JsonSerializerOptionsComparer.AreEqual(options1, options2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_ShouldReturnFalse_WhenNewReflectionResolverIsSet()
    {
        // The DefaultJsonTypeInfoResolver instances are compared by reference, as any other resolver
        JsonSerializerOptions options1 = new() { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
        JsonSerializerOptions options2 = new();

        bool result = JsonSerializerOptionsComparer.AreEqual(options1, options2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void AreEqual_ShouldReturnFalse_WhenCustomEnumerableResolverHasSameItems()
    {
        JsonSerializerOptions options1 = new()
        {
            TypeInfoResolver = new TestEnumerableResolver(TestEventOneJsonSerializerContext.Default, TestEventTwoJsonSerializerContext.Default)
        };
        JsonSerializerOptions options2 = new()
        {
            TypeInfoResolver = JsonTypeInfoResolver.Combine(
                TestEventOneJsonSerializerContext.Default,
                TestEventTwoJsonSerializerContext.Default)
        };

        bool result = JsonSerializerOptionsComparer.AreEqual(options1, options2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void AreEqual_ShouldReturnFalse_WhenDefaultResolverHasModifiers()
    {
        DefaultJsonTypeInfoResolver resolver = new();
        resolver.Modifiers.Add(_ => { });
        JsonSerializerOptions options1 = new() { TypeInfoResolver = resolver };
        JsonSerializerOptions options2 = new();

        bool result = JsonSerializerOptionsComparer.AreEqual(options1, options2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void AreEqual_ShouldReturnTrue_WhenSameTypeInfoResolver()
    {
        JsonSerializerOptions options1 = new() { TypeInfoResolver = TestEventOneJsonSerializerContext.Default };
        JsonSerializerOptions options2 = new();
        options2.TypeInfoResolverChain.Add(TestEventOneJsonSerializerContext.Default);

        bool result = JsonSerializerOptionsComparer.AreEqual(options1, options2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_ShouldReturnFalse_WhenDifferentTypeInfoResolver()
    {
        JsonSerializerOptions options1 = new() { TypeInfoResolver = TestEventOneJsonSerializerContext.Default };
        JsonSerializerOptions options2 = new() { TypeInfoResolver = TestEventTwoJsonSerializerContext.Default };

        bool result = JsonSerializerOptionsComparer.AreEqual(options1, options2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void AreEqual_ShouldReturnFalse_WhenComparingTypeInfoResolverWithDefault()
    {
        JsonSerializerOptions options1 = new() { TypeInfoResolver = TestEventOneJsonSerializerContext.Default };
        JsonSerializerOptions options2 = new();

        bool result = JsonSerializerOptionsComparer.AreEqual(options1, options2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void AreEqual_ShouldReturnTrue_WhenSameTypeInfoResolverChain()
    {
        JsonSerializerOptions options1 = new();
        options1.TypeInfoResolverChain.Add(TestEventOneJsonSerializerContext.Default);
        options1.TypeInfoResolverChain.Add(TestEventTwoJsonSerializerContext.Default);
        JsonSerializerOptions options2 = new()
        {
            TypeInfoResolver = JsonTypeInfoResolver.Combine(
                TestEventOneJsonSerializerContext.Default,
                TestEventTwoJsonSerializerContext.Default)
        };

        bool result = JsonSerializerOptionsComparer.AreEqual(options1, options2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_ShouldReturnTrue_WhenSameTypeInfoResolverChainIsNested()
    {
        JsonSerializerOptions options1 = new();
        options1.TypeInfoResolverChain.Add(
            JsonTypeInfoResolver.Combine(
                TestEventOneJsonSerializerContext.Default,
                TestEventTwoJsonSerializerContext.Default));
        JsonSerializerOptions options2 = new();
        options2.TypeInfoResolverChain.Add(TestEventOneJsonSerializerContext.Default);
        options2.TypeInfoResolverChain.Add(TestEventTwoJsonSerializerContext.Default);

        bool result = JsonSerializerOptionsComparer.AreEqual(options1, options2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_ShouldReturnFalse_WhenDifferentTypeInfoResolverChainOrder()
    {
        JsonSerializerOptions options1 = new();
        options1.TypeInfoResolverChain.Add(TestEventOneJsonSerializerContext.Default);
        options1.TypeInfoResolverChain.Add(TestEventTwoJsonSerializerContext.Default);
        JsonSerializerOptions options2 = new();
        options2.TypeInfoResolverChain.Add(TestEventTwoJsonSerializerContext.Default);
        options2.TypeInfoResolverChain.Add(TestEventOneJsonSerializerContext.Default);

        bool result = JsonSerializerOptionsComparer.AreEqual(options1, options2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void AreEqual_ShouldReturnFalse_WhenDifferentTypeInfoResolverChainFallback()
    {
        JsonSerializerOptions options1 = new();
        options1.TypeInfoResolverChain.Add(TestEventOneJsonSerializerContext.Default);
        options1.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver());
        JsonSerializerOptions options2 = new();
        options2.TypeInfoResolverChain.Add(TestEventOneJsonSerializerContext.Default);
        options2.TypeInfoResolverChain.Add(TestEventTwoJsonSerializerContext.Default);

        bool result = JsonSerializerOptionsComparer.AreEqual(options1, options2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void AreEqual_ShouldReturnTrue_WhenSameNamingPolicy()
    {
        JsonSerializerOptions options1 = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        JsonSerializerOptions options2 = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        bool result = JsonSerializerOptionsComparer.AreEqual(options1, options2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_ShouldReturnFalse_WhenDifferentNamingPolicy()
    {
        JsonSerializerOptions options1 = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        JsonSerializerOptions options2 = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

        bool result = JsonSerializerOptionsComparer.AreEqual(options1, options2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void AreEqual_ShouldReturnFalse_WhenDifferentReferenceHandler()
    {
        JsonSerializerOptions options1 = new() { ReferenceHandler = ReferenceHandler.Preserve };
        JsonSerializerOptions options2 = new() { ReferenceHandler = ReferenceHandler.IgnoreCycles };

        bool result = JsonSerializerOptionsComparer.AreEqual(options1, options2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void AreEqual_ShouldReturnTrue_WhenSameConverters()
    {
        JsonStringEnumConverter converter = new();
        JsonSerializerOptions options1 = new();
        options1.Converters.Add(converter);
        JsonSerializerOptions options2 = new();
        options2.Converters.Add(converter);

        bool result = JsonSerializerOptionsComparer.AreEqual(options1, options2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_ShouldReturnFalse_WhenDifferentConvertersOrder()
    {
        JsonStringEnumConverter converter1 = new();
        JsonStringEnumConverter converter2 = new(JsonNamingPolicy.CamelCase);
        JsonSerializerOptions options1 = new();
        options1.Converters.Add(converter1);
        options1.Converters.Add(converter2);
        JsonSerializerOptions options2 = new();
        options2.Converters.Add(converter2);
        options2.Converters.Add(converter1);

        bool result = JsonSerializerOptionsComparer.AreEqual(options1, options2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void AreEqual_ShouldReturnFalse_WhenDifferentConverters()
    {
        JsonSerializerOptions options1 = new();
        options1.Converters.Add(new JsonStringEnumConverter());
        JsonSerializerOptions options2 = new();
        options2.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        bool result = JsonSerializerOptionsComparer.AreEqual(options1, options2);

        result.ShouldBeFalse();
    }

    private sealed class TestEnumerableResolver : IJsonTypeInfoResolver, IEnumerable<IJsonTypeInfoResolver>
    {
        private readonly IJsonTypeInfoResolver[] _resolvers;

        public TestEnumerableResolver(params IJsonTypeInfoResolver[] resolvers)
        {
            _resolvers = resolvers;
        }

        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options) =>
            _resolvers.Reverse().Select(resolver => resolver.GetTypeInfo(type, options)).FirstOrDefault(typeInfo => typeInfo != null);

        public IEnumerator<IJsonTypeInfoResolver> GetEnumerator() => ((IEnumerable<IJsonTypeInfoResolver>)_resolvers).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
