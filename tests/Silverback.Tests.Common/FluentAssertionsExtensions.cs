// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using FluentAssertions.Primitives;

namespace Silverback.Tests;

public static class FluentAssertionsExtensions
{
    public static AndConstraint<ObjectAssertions> ShouldNotBeNull([NotNull] this object? actualValue)
    {
        AndConstraint<ObjectAssertions>? result = actualValue.Should().NotBeNull();

        ArgumentNullException.ThrowIfNull(actualValue);

        return result;
    }
}
