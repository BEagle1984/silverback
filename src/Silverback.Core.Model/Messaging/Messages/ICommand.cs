// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Represents a message that triggers an action.
    /// </summary>
    [SuppressMessage("", "CA1040", Justification = Justifications.MarkerInterface)]
    public interface ICommand : IMessage
    {
    }
}