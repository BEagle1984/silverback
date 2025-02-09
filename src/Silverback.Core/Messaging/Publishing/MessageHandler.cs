// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Messaging.Publishing
{
    /// <summary>
    ///     The delegate representing the <c>Handle</c> method of the <see cref="IBehavior" />.
    /// </summary>
    /// <param name="message">
    ///     The message being published.
    /// </param>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
    ///     actual messages to be published.
    /// </returns>
    public delegate Task<IReadOnlyCollection<object?>> MessageHandler(
        object message,
        CancellationToken cancellationToken = default);
}
