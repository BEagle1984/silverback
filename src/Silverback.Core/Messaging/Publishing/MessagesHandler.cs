// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Messaging.Publishing
{
    /// <summary>
    ///     The delegate representing the <c>
    ///         Handle
    ///     </c> method of the <see cref="IBehavior" />.
    /// </summary>
    /// <param name="messages">
    ///     The messages being published.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation. The task result contains the actual
    ///     messages to be published.
    /// </returns>
    public delegate Task<IReadOnlyCollection<object?>> MessagesHandler(IReadOnlyCollection<object> messages);
}
