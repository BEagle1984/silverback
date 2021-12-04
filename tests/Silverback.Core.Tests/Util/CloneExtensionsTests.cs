// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class CloneExtensionsTests
{
    [Fact]
    public void ShallowCopy_SomeObject_ShallowCloneReturned()
    {
        SomeObject source = new()
        {
            Number = 42,
            Nested = new SomeNestedObject
            {
                Text = "Silverback",
                Nested = new SomeNestedNestedObject
                {
                    Number = 4.2
                }
            }
        };

        SomeObject clone = source.ShallowCopy();

        clone.Should().NotBeSameAs(source);
        clone.Should().BeEquivalentTo(source);

        clone.Nested.Should().BeSameAs(source.Nested);
    }

    private sealed class SomeObject
    {
        public int Number { get; set; }

        public SomeNestedObject? Nested { get; set; }
    }

    private sealed class SomeNestedObject
    {
        public string? Text { get; set; }

        public SomeNestedNestedObject? Nested { get; set; }
    }

    private sealed class SomeNestedNestedObject
    {
        public double Number { get; set; }
    }
}
