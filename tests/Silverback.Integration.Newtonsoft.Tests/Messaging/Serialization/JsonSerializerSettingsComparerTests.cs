// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Shouldly;
using Silverback.Messaging.Serialization;
using Xunit;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace Silverback.Tests.Integration.Newtonsoft.Messaging.Serialization;

public class JsonSerializerSettingsComparerTests
{
    [Fact]
    public void AreEqual_ShouldReturnTrue_WhenBothNull()
    {
        bool result = JsonSerializerSettingsComparer.AreEqual(null, null);

        result.ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_ShouldReturnTrue_WhenComparingNullWithDefaultSettings()
    {
        bool result = JsonSerializerSettingsComparer.AreEqual(null, new JsonSerializerSettings());

        result.ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_ShouldReturnTrue_WhenSameSettings()
    {
        JsonSerializerSettings settings1 = new()
        {
            MaxDepth = 42,
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatString = "yyyy-MM-dd"
        };
        JsonSerializerSettings settings2 = new()
        {
            MaxDepth = 42,
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatString = "yyyy-MM-dd"
        };

        bool result = JsonSerializerSettingsComparer.AreEqual(settings1, settings2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_ShouldReturnFalse_WhenDifferentSettings()
    {
        JsonSerializerSettings settings1 = new() { MaxDepth = 42 };
        JsonSerializerSettings settings2 = new() { MaxDepth = 13 };

        bool result = JsonSerializerSettingsComparer.AreEqual(settings1, settings2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void AreEqual_ShouldReturnFalse_WhenComparingNullWithCustomSettings()
    {
        bool result = JsonSerializerSettingsComparer.AreEqual(null, new JsonSerializerSettings { MaxDepth = 42 });

        result.ShouldBeFalse();
    }

    [Fact]
    public void AreEqual_ShouldReturnFalse_WhenSettingIsExplicitlySetToDefault()
    {
        // The unset settings fall back to JsonConvert.DefaultSettings, the explicitly set ones don't
        JsonSerializerSettings settings1 = new() { Formatting = Formatting.None };
        JsonSerializerSettings settings2 = new();

        bool result = JsonSerializerSettingsComparer.AreEqual(settings1, settings2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void AreEqual_ShouldReturnTrue_WhenComparingWithCopy()
    {
        JsonSerializerSettings settings1 = new()
        {
            MaxDepth = 42,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        settings1.Converters.Add(new StringEnumConverter());
        JsonSerializerSettings settings2 = new(settings1);

        bool result = JsonSerializerSettingsComparer.AreEqual(settings1, settings2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_ShouldReturnTrue_WhenSameContractResolver()
    {
        CamelCasePropertyNamesContractResolver contractResolver = new();
        JsonSerializerSettings settings1 = new() { ContractResolver = contractResolver };
        JsonSerializerSettings settings2 = new() { ContractResolver = contractResolver };

        bool result = JsonSerializerSettingsComparer.AreEqual(settings1, settings2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_ShouldReturnFalse_WhenDifferentContractResolver()
    {
        JsonSerializerSettings settings1 = new() { ContractResolver = new CamelCasePropertyNamesContractResolver() };
        JsonSerializerSettings settings2 = new() { ContractResolver = new DefaultContractResolver() };

        bool result = JsonSerializerSettingsComparer.AreEqual(settings1, settings2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void AreEqual_ShouldReturnTrue_WhenSameConverters()
    {
        StringEnumConverter converter = new();
        JsonSerializerSettings settings1 = new();
        settings1.Converters.Add(converter);
        JsonSerializerSettings settings2 = new();
        settings2.Converters.Add(converter);

        bool result = JsonSerializerSettingsComparer.AreEqual(settings1, settings2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_ShouldReturnFalse_WhenDifferentConverters()
    {
        JsonSerializerSettings settings1 = new();
        settings1.Converters.Add(new StringEnumConverter());
        JsonSerializerSettings settings2 = new();
        settings2.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));

        bool result = JsonSerializerSettingsComparer.AreEqual(settings1, settings2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void AreEqual_ShouldReturnTrue_WhenSameSerializationBinder()
    {
        DefaultSerializationBinder binder = new();
        JsonSerializerSettings settings1 = new() { SerializationBinder = binder };
        JsonSerializerSettings settings2 = new() { SerializationBinder = binder };

        bool result = JsonSerializerSettingsComparer.AreEqual(settings1, settings2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_ShouldReturnFalse_WhenDifferentSerializationBinder()
    {
        JsonSerializerSettings settings1 = new() { SerializationBinder = new DefaultSerializationBinder() };
        JsonSerializerSettings settings2 = new() { SerializationBinder = new TestSerializationBinder() };

        bool result = JsonSerializerSettingsComparer.AreEqual(settings1, settings2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void AreEqual_ShouldReturnTrue_WhenSameCulture()
    {
        JsonSerializerSettings settings1 = new() { Culture = CultureInfo.GetCultureInfo("de-CH") };
        JsonSerializerSettings settings2 = new() { Culture = CultureInfo.GetCultureInfo("de-CH") };

        bool result = JsonSerializerSettingsComparer.AreEqual(settings1, settings2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_ShouldReturnFalse_WhenCultureHasDifferentFormat()
    {
        CultureInfo culture = new("en-US");
        culture.NumberFormat.NumberDecimalSeparator = ",";
        JsonSerializerSettings settings1 = new() { Culture = culture };
        JsonSerializerSettings settings2 = new() { Culture = CultureInfo.GetCultureInfo("en-US") };

        bool result = JsonSerializerSettingsComparer.AreEqual(settings1, settings2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void AreEqual_ShouldReturnFalse_WhenMaxDepthIsExplicitlySetToNull()
    {
        // The unset MaxDepth falls back to JsonConvert.DefaultSettings, the explicitly set one doesn't
        JsonSerializerSettings settings1 = new() { MaxDepth = null };
        JsonSerializerSettings settings2 = new();

        bool result = JsonSerializerSettingsComparer.AreEqual(settings1, settings2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void AreEqual_ShouldReturnTrue_WhenSameErrorHandlerMethod()
    {
        JsonSerializerSettings settings1 = new() { Error = HandleError };
        JsonSerializerSettings settings2 = new() { Error = HandleError };

        bool result = JsonSerializerSettingsComparer.AreEqual(settings1, settings2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_ShouldReturnFalse_WhenDifferentErrorHandler()
    {
        JsonSerializerSettings settings1 = new() { Error = (_, args) => args.ErrorContext.Handled = true };
        JsonSerializerSettings settings2 = new();

        bool result = JsonSerializerSettingsComparer.AreEqual(settings1, settings2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void AreEqual_ShouldNotInvokeReferenceResolverProvider()
    {
        int invocations = 0;
        JsonSerializerSettings settings1 = new()
        {
            ReferenceResolverProvider = () =>
            {
                invocations++;
                return null!;
            }
        };
        JsonSerializerSettings settings2 = new();

        JsonSerializerSettingsComparer.AreEqual(settings1, settings2);

        invocations.ShouldBe(0);
    }

    [Fact]
    public void AreEqual_ShouldReturnFalse_WhenSubclassedSettings()
    {
        JsonSerializerSettings settings1 = new TestSettings();
        JsonSerializerSettings settings2 = new TestSettings();

        bool result = JsonSerializerSettingsComparer.AreEqual(settings1, settings2);

        result.ShouldBeFalse();
    }

    private static void HandleError(object? sender, ErrorEventArgs args) => args.ErrorContext.Handled = true;

    private sealed class TestSettings : JsonSerializerSettings;

    private sealed class TestSerializationBinder : DefaultSerializationBinder;
}
