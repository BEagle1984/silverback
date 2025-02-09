// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Shouldly;
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

        collection[1].ShouldBe(new MessageHeader("two", "2"));
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

        act.ShouldThrow<ArgumentOutOfRangeException>();
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

        collection.ShouldBe(
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

        collection["two"].ShouldBe("2");
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

        act.ShouldThrow<ArgumentOutOfRangeException>();
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

        collection.ShouldBe(
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

        collection.ShouldBe(
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
        MessageHeaderCollection collection = [];

        collection.Add("one", 1);
        collection.Add("two", 2);
        collection.Add("three", 3);

        collection.ShouldBe(
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
        MessageHeaderCollection collection = [];

        collection.Add("one", "1");
        collection.Add("two", "2");
        collection.Add("three", "3");

        collection.ShouldBe(
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
        MessageHeaderCollection collection =
        [
            new MessageHeader("one", "1"),
            new MessageHeader("two", "2"),
            new MessageHeader("three", "3")
        ];

        collection.ShouldBe(
            new[]
            {
                new MessageHeader("one", "1"),
                new MessageHeader("two", "2"),
                new MessageHeader("three", "3")
            });
    }

    [Fact]
    public void Add_ShouldConvertDateTimeToStringWithInvariantCulture()
    {
        MessageHeaderCollection collection = [];

        collection.Add("one", new DateTime(2023, 6, 23, 2, 42, 42));

        collection.ShouldBe(
            new[]
            {
                new MessageHeader("one", "2023-06-23T02:42:42.0000000")
            });
    }

    [Fact]
    public void AddRange_ShouldAddMessageHeader()
    {
        MessageHeaderCollection collection = [];

        collection.AddRange(new MessageHeaderCollection { { "one", "1" }, { "two", "2" }, { "three", "3" } });

        collection.ShouldBe(
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

        affected.ShouldBe(1);
        collection.ShouldBe(
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

        affected.ShouldBe(1);
        collection.ShouldBe(
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

        affected.ShouldBe(0);
        collection.ShouldBe(
            new[]
            {
                new MessageHeader("one", "1"),
                new MessageHeader("two", "2")
            });
    }

    [Fact]
    public void Replace_ShouldConvertDateTimeToStringWithInvariantCulture()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", new DateTime(2023, 1, 1) }
        };

        collection.Replace("one", new DateTime(2023, 6, 23, 2, 42, 43));

        collection.ShouldBe(
            new[]
            {
                new MessageHeader("one", "2023-06-23T02:42:43.0000000")
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

        collection.ShouldBe(
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

        collection.ShouldBe(
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

        collection.ShouldBe(
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

        collection.ShouldBe(
            new[]
            {
                new MessageHeader("one", "1"),
                new MessageHeader("two", "2"),
                new MessageHeader("three", "3")
            });
    }

    [Fact]
    public void AddOrReplace_ShouldConvertDateTimeToStringWithInvariantCulture()
    {
        MessageHeaderCollection collection = new()
        {
            { "one", new DateTime(2023, 1, 1) }
        };

        collection.AddOrReplace("one", new DateTime(2023, 6, 23, 2, 42, 43));

        collection.ShouldBe(
            new[]
            {
                new MessageHeader("one", "2023-06-23T02:42:43.0000000")
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

        collection.ShouldBe(
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

        collection.ShouldBe(
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

        collection.ShouldBe(
            new[]
            {
                new MessageHeader("one", "1"),
                new MessageHeader("two", "2"),
                new MessageHeader("three", "3")
            });
    }

    [Fact]
    public void AddIfNotExists_ShouldConvertDateTimeToStringWithInvariantCulture()
    {
        MessageHeaderCollection collection = [];

        collection.AddIfNotExists("one", new DateTime(2023, 6, 23, 2, 42, 43));

        collection.ShouldBe(
            new[]
            {
                new MessageHeader("one", "2023-06-23T02:42:43.0000000")
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

        collection.ShouldBe(
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

        collection.ShouldBe(
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

        collection.ShouldBe(
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

        collection.ShouldBe(
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

        collection.Contains("one").ShouldBeTrue();
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

        collection.Contains("four").ShouldBeFalse();
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

        result.ShouldBeTrue();
        value.ShouldBe("2");
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

        result.ShouldBeTrue();
        value.ShouldBeNull();
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

        result.ShouldBeFalse();
        value.ShouldBeNull();
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

        collection.GetValue("two").ShouldBe("2");
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

        collection.GetValue("four").ShouldBeNull();
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

        result.ShouldBe(2);
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

        result.ShouldBe(2);
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

        result.ShouldBeNull();
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

        result.ShouldBeNull();
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

        collection.GetValueOrDefault("two", typeof(int)).ShouldBe(2);
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

        collection.GetValueOrDefault("four", typeof(int)).ShouldBe(0);
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

        result.ShouldBe(2);
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

        result.ShouldBeOfType(typeof(int));
        result.ShouldBe(2);
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

        result.ShouldBe(0);
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

        result.ShouldBeOfType(typeof(int));
        result.ShouldBe(0);
    }
}
