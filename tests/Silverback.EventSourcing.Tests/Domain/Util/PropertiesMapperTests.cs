// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Domain.Util;
using Xunit;

namespace Silverback.Tests.EventSourcing.Domain.Util;

public class PropertiesMapperTests
{
    [Fact]
    public void Map_MatchingNames_PropertiesValuesCopied()
    {
        var source = new { Id = 123, Title = "Silverback for Dummies", Published = DateTime.Today, Pages = 13 };
        Book dest = new();

        PropertiesMapper.Map(source, dest);

        dest.Id.Should().Be(123);
        dest.Title.Should().Be("Silverback for Dummies");
        dest.Published.Should().Be(DateTime.Today);
        dest.Pages.Should().Be(13);
    }

    [Fact]
    public void Map_PrefixedNames_PropertiesValuesCopied()
    {
        var source = new { EntityId = 123, BookTitle = "Silverback for Dummies" };
        Book dest = new();

        PropertiesMapper.Map(source, dest);

        dest.Id.Should().Be(123);
        dest.Title.Should().Be("Silverback for Dummies");
    }

    [Fact]
    public void Map_NonMatchingNames_PropertiesValuesNotCopied()
    {
        var source = new { Key = 123, Price = 13.5 };
        Book dest = new();

        Action act = () => PropertiesMapper.Map(source, dest);

        act.Should().NotThrow();
    }

    private sealed class Book
    {
        public int Id { get; private set; }

        public string? Title { get; private set; }

        public DateTime Published { get; set; }

        public int? Pages { get; set; }
    }
}
