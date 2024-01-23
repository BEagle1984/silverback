// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Messages;

[SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "Make the tested method evident")]
public class MessageHeaderCollectionFixture
{
    [Fact]
    public void IntIndexer_ShouldReturnHeader()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" }
        };

        collection[1].Should().BeEquivalentTo(new MessageHeader("two", "2"));
    }

    [Fact]
    public void IntIndexer_ShouldThrow_WhenSettingValueOfOutOfRangeIndex()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" }
        };

        Action act = () => collection[4] = new MessageHeader("four", "4");

        act.Should().ThrowExactly<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void IntIndexer_ShouldReplaceHeaderValue_WhenSettingValueOfExistingHeader()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" }
        };

        collection[1] = new MessageHeader("two!", "2!");

        collection.Should().BeEquivalentTo(
            new[]
            {
                new MessageHeader("one", "1"),
                new MessageHeader("two!", "2!"),
                new MessageHeader("three", "3")
            });
    }

    [Fact]
    public void StringIndexer_ShouldReturnHeader()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" }
        };

        collection["two"].Should().BeEquivalentTo("2");
    }

    [Fact]
    public void StringIndexer_ShouldThrow_WhenGettingHeaderThatDoesNotExist()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" }
        };

        Func<string?> act = () => collection["four"];

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void StringIndexer_ShouldAddHeader_WhenSettingValueOfHeaderThatDoesNotExist()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" }
        };

        collection["four"] = "4";

        collection.Should().BeEquivalentTo(
            new[]
            {
                new MessageHeader("one", "1"),
                new MessageHeader("two", "2"),
                new MessageHeader("three", "3"),
                new MessageHeader("four", "4")
            });
    }

    [Fact]
    public void StringIndexer_ShouldReplaceHeaderValue_WhenSettingValueOfExistingHeader()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" }
        };

        collection["two"] = "2!";

        collection.Should().BeEquivalentTo(
            new[]
            {
                new MessageHeader("one", "1"),
                new MessageHeader("two", "2!"),
                new MessageHeader("three", "3")
            });
    }

    [Fact]
    public void Add_ShouldAddHeaderWithIntValue()
    {
        MessageHeaderCollection collection = new();

        collection.Add("one", 1);
        collection.Add("two", 2);
        collection.Add("three", 3);

        collection.Should().BeEquivalentTo(
            new[]
            {
                new MessageHeader("one", "1"),
                new MessageHeader("two", "2"),
                new MessageHeader("three", "3")
            });
    }

    [Fact]
    public void Add_ShouldAddHeaderWithStringValue()
    {
        MessageHeaderCollection collection = new();

        collection.Add("one", "1");
        collection.Add("two", "2");
        collection.Add("three", "3");

        collection.Should().BeEquivalentTo(
            new[]
            {
                new MessageHeader("one", "1"),
                new MessageHeader("two", "2"),
                new MessageHeader("three", "3")
            });
    }

    [Fact]
    public void Add_ShouldAddMessageHeader()
    {
        MessageHeaderCollection collection = new();

        collection.Add(new MessageHeader("one", "1"));
        collection.Add(new MessageHeader("two", "2"));
        collection.Add(new MessageHeader("three", "3"));

        collection.Should().BeEquivalentTo(
            new[]
            {
                new MessageHeader("one", "1"),
                new MessageHeader("two", "2"),
                new MessageHeader("three", "3")
            });
    }

    [Fact]
    public void AddRange_ShouldAddMessageHeader()
    {
        MessageHeaderCollection collection = new();

        collection.AddRange(new MessageHeaderCollection { { "one", "1" }, { "two", "2" }, { "three", "3" } });

        collection.Should().BeEquivalentTo(
            new[]
            {
                new MessageHeader("one", "1"),
                new MessageHeader("two", "2"),
                new MessageHeader("three", "3")
            });
    }

    [Fact]
    public void Replace_ShouldReplaceIntValue_WhenHeaderAlreadyExists()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" }
        };

        int affected = collection.Replace("one", 12);

        affected.Should().Be(1);
        collection.Should().BeEquivalentTo(
            new[]
            {
                new MessageHeader("one", "12"),
                new MessageHeader("two", "2")
            });
    }

    [Fact]
    public void Replace_ShouldReplaceStringValue_WhenHeaderAlreadyExists()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" }
        };

        int affected = collection.Replace("one", "1(2)");

        affected.Should().Be(1);
        collection.Should().BeEquivalentTo(
            new[]
            {
                new MessageHeader("one", "1(2)"),
                new MessageHeader("two", "2")
            });
    }

    [Fact]
    public void Replace_ShouldDoNothing_WhenHeaderDoesNotExist()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" }
        };

        int affected = collection.Replace("three", "3");

        affected.Should().Be(0);
        collection.Should().BeEquivalentTo(
            new[]
            {
                new MessageHeader("one", "1"),
                new MessageHeader("two", "2")
            });
    }

    [Fact]
    public void AddOrReplace_ShouldReplaceWithIntValue_WhenHeaderAlreadyExist()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" }
        };

        collection.AddOrReplace("one", 12);

        collection.Should().BeEquivalentTo(
            new[]
            {
                new MessageHeader("one", "12"),
                new MessageHeader("two", "2")
            });
    }

    [Fact]
    public void AddOrReplace_ShouldReplaceWithStringValue_WhenHeaderAlreadyExist()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" }
        };

        collection.AddOrReplace("one", "1(2)");

        collection.Should().BeEquivalentTo(
            new[]
            {
                new MessageHeader("one", "1(2)"),
                new MessageHeader("two", "2")
            });
    }

    [Fact]
    public void AddOrReplace_ShouldAddHeaderWithIntValue_WhenHeaderDoesNotExist()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" }
        };

        collection.AddOrReplace("three", 3);

        collection.Should().BeEquivalentTo(
            new[]
            {
                new MessageHeader("one", "1"),
                new MessageHeader("two", "2"),
                new MessageHeader("three", "3")
            });
    }

    [Fact]
    public void AddOrReplace_ShouldAddHeaderWithStringValue_WhenHeaderDoesNotExist()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" }
        };

        collection.AddOrReplace("three", "3");

        collection.Should().BeEquivalentTo(
            new[]
            {
                new MessageHeader("one", "1"),
                new MessageHeader("two", "2"),
                new MessageHeader("three", "3")
            });
    }

    [Fact]
    public void AddIfNotExists_ShouldDoNothing_WhenHeaderExists()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" }
        };

        collection.AddIfNotExists("one", "1(2)");

        collection.Should().BeEquivalentTo(
            new[]
            {
                new MessageHeader("one", "1"),
                new MessageHeader("two", "2")
            });
    }

    [Fact]
    public void AddIfNotExists_ShouldAddHeaderWithIntValue_WhenHeaderDoesNotExist()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" }
        };

        collection.AddIfNotExists("three", 3);

        collection.Should().BeEquivalentTo(
            new[]
            {
                new MessageHeader("one", "1"),
                new MessageHeader("two", "2"),
                new MessageHeader("three", "3")
            });
    }

    [Fact]
    public void AddIfNotExists_ShouldAddHeaderWithStringValue_WhenHeaderDoesNotExist()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" }
        };

        collection.AddIfNotExists("three", "3");

        collection.Should().BeEquivalentTo(
            new[]
            {
                new MessageHeader("one", "1"),
                new MessageHeader("two", "2"),
                new MessageHeader("three", "3")
            });
    }

    [Fact]
    public void Remove_ShouldRemoveHeader_WhenHeaderNameExists()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" }
        };

        collection.Remove("two");

        collection.Should().BeEquivalentTo(
            new[]
            {
                new MessageHeader("one", "1"),
                new MessageHeader("three", "3")
            });
    }

    [Fact]
    public void Remove_ShouldDoNothing_WhenHeaderNameDoesNotExist()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" }
        };

        collection.Remove("four");

        collection.Should().BeEquivalentTo(
            new[]
            {
                new MessageHeader("one", "1"),
                new MessageHeader("two", "2"),
                new MessageHeader("three", "3")
            });
    }

    [Fact]
    public void Remove_ShouldRemoveHeader_WhenHeaderExists()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" }
        };

        collection.Remove(new MessageHeader("two", "2"));

        collection.Should().BeEquivalentTo(
            new[]
            {
                new MessageHeader("one", "1"),
                new MessageHeader("three", "3")
            });
    }

    [Fact]
    public void Remove_ShouldDoNothing_WhenHeaderDoesNotExist()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" }
        };

        collection.Remove(new MessageHeader("four", "4"));

        collection.Should().BeEquivalentTo(
            new[]
            {
                new MessageHeader("one", "1"),
                new MessageHeader("two", "2"),
                new MessageHeader("three", "3")
            });
    }

    [Fact]
    public void Contains_ShouldReturnTrue_WhenHeaderExists()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" }
        };

        collection.Contains("one").Should().BeTrue();
    }

    [Fact]
    public void Contains_ShouldReturnFalse_WhenHeaderDoesNotExist()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" }
        };

        collection.Contains("four").Should().BeFalse();
    }

    [Fact]
    public void TryGetValue_ShouldReturnTrueAndValue()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" }
        };

        bool result = collection.TryGetValue("two", out string? value);

        result.Should().BeTrue();
        value.Should().BeEquivalentTo("2");
    }

    [Fact]
    public void TryGetValue_ShouldReturnTrueAndNull_WhenHeaderValueIsNull()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", null },
            { "three", "3" }
        };

        bool result = collection.TryGetValue("two", out string? value);

        result.Should().BeTrue();
        value.Should().BeNull();
    }

    [Fact]
    public void TryGetValue_ShouldReturnFalse_WhenHeaderDoesNotExist()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" }
        };

        bool result = collection.TryGetValue("four", out string? value);

        result.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void GetValue_ShouldReturnValue()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" }
        };

        collection.GetValue("two").Should().BeEquivalentTo("2");
    }

    [Fact]
    public void GetValue_ShouldReturnNull_WhenHeaderDoesNotExist()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" }
        };

        collection.GetValue("four").Should().BeEquivalentTo(null);
    }

    [Fact]
    public void GetValue_ShouldReturnNullableValueType_WhenTypeSpecifiedViaGenericArgument()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" }
        };

        int? result = collection.GetValue<int>("two");

        result.Should().Be(2);
    }

    [Fact]
    public void GetValue_ShouldReturnNullableValueType_WhenTypeSpecifiedViaParameter()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" }
        };

        int? result = (int?)collection.GetValue("two", typeof(int));

        result.Should().Be(2);
    }

    [Fact]
    public void GetValue_ShouldReturnNull_WhenTypeSpecifiedViaGenericArgumentAndHeaderDoesNotExist()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" }
        };

        int? result = collection.GetValue<int>("four");

        result.Should().BeNull();
    }

    [Fact]
    public void GetValue_ShouldReturnNull_WhenTypeSpecifiedViaParameterAndHeaderDoesNotExist()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" }
        };

        int? result = (int?)collection.GetValue("four", typeof(int));

        result.Should().BeNull();
    }

    [Fact]
    public void GetValueOrDefault_ShouldReturnValue()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" }
        };

        collection.GetValueOrDefault("two", typeof(int)).Should().Be(2);
    }

    [Fact]
    public void GetValueOrDefault_ShouldReturnDefault_WhenHeaderDoesNotExist()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" }
        };

        collection.GetValueOrDefault("four", typeof(int)).Should().Be(0);
    }

    [Fact]
    public void GetValueOrDefault_ShouldReturnConvertedValue_WhenTypeSpecifiedViaGenericArgument()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" }
        };

        int result = collection.GetValueOrDefault<int>("two");

        result.Should().Be(2);
    }

    [Fact]
    public void GetValueOrDefault_ShouldReturnConvertedValue_WhenTypeSpecifiedViaParameter()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" }
        };

        object? result = collection.GetValueOrDefault("two", typeof(int));

        result.Should().BeOfType(typeof(int));
        result.Should().Be(2);
    }

    [Fact]
    public void GetValueOrDefault_ShouldReturnDefaultValue_WhenTypeSpecifiedViaGenericArgumentAndHeaderDoesNotExist()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" }
        };

        int result = collection.GetValueOrDefault<int>("four");

        result.Should().Be(0);
    }

    [Fact]
    public void GetValueOrDefault_ShouldReturnDefaultValue_WhenTypeSpecifiedViaParameterAndHeaderDoesNotExist()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", "1" },
            { "two", "2" },
            { "three", "3" }
        };

        object? result = collection.GetValueOrDefault("four", typeof(int));

        result.Should().BeOfType(typeof(int));
        result.Should().Be(0);
    }
}
