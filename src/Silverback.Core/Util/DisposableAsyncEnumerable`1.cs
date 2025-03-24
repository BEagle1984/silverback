// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;

namespace Silverback.Util;

/// <inheritdoc cref="IDisposableAsyncEnumerable{T}" />
public class DisposableAsyncEnumerable<T> : IDisposableAsyncEnumerable<T>
{
    private readonly IAsyncEnumerable<T> _wrappedAsyncEnumerable;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DisposableAsyncEnumerable{T}" /> class.
    /// </summary>
    /// <param name="wrappedAsyncEnumerable">
    ///     The <see cref="IAsyncEnumerable{T}" /> to be wrapped.
    /// </param>
    public DisposableAsyncEnumerable(IAsyncEnumerable<T> wrappedAsyncEnumerable)
    {
        _wrappedAsyncEnumerable = wrappedAsyncEnumerable;
    }

    /// <inheritdoc cref="IAsyncEnumerable{T}.GetAsyncEnumerator" />
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        _wrappedAsyncEnumerable.GetAsyncEnumerator(cancellationToken);

    /// <inheritdoc cref="IDisposable.Dispose" />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <param name="disposing">
    ///     A value indicating whether the method has been called by the <c>Dispose</c> method and not from the finalizer.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        // Nothing to dispose by default
    }
}
