// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     A message that notifies an event.
    /// </summary>
    [SuppressMessage("", "CA1040", Justification = Justifications.MarkerInterface)]
    public interface IEvent : IMessage
    {
    }
}
