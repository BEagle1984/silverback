// Copyright (c) 2023 Sergio Aquilini
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

        if (actualValue == null)
        {
            throw new ArgumentNullException(nameof(actualValue)); // Will never be thrown, needed only to trick the compiler
        }

        return result;
    }
}
