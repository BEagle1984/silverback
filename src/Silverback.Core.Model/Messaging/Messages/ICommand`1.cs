// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Represents a message that triggers an action with a result <typeparamref name="TResult" />.
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result being returned.
    /// </typeparam>
    [SuppressMessage("", "CA1040", Justification = Justifications.MarkerInterface)]
    [SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Used by the Publisher")]
    [SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Used by the Publisher")]
    public interface ICommand<out TResult> : ICommand
    {
    }
}
