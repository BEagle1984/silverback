// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Represent an event that is triggered internally by Silverback.
    /// </summary>
    [SuppressMessage("ReSharper", "CA1040", Justification = Justifications.MarkerInterface)]
    public interface ISilverbackEvent : IMessage
    {
    }
}