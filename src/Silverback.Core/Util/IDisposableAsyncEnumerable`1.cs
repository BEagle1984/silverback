// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Util;

/// <summary>
///     Wraps an <see cref="IAsyncEnumerable{T}" /> and can (and must) be disposed after enumeration.
/// </summary>
/// <typeparam name="T">
///     The type of the elements in the collection.
/// </typeparam>
public interface IDisposableAsyncEnumerable<out T> : IAsyncEnumerable<T>, IDisposable;
