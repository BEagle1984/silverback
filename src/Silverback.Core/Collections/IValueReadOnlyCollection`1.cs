// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Collections;

/// <summary>
///     Represents a strongly-typed, read-only collection of elements that implements a value equality.
/// </summary>
/// <typeparam name="T">
///     The type of the elements.
/// </typeparam>
public interface IValueReadOnlyCollection<out T> : IReadOnlyCollection<T>
{
}
