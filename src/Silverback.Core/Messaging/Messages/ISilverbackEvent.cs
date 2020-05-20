// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     An event that is triggered internally by Silverback.
    /// </summary>
    [SuppressMessage("", "CA1040", Justification = Justifications.BaseInterface)]
    public interface ISilverbackEvent : IMessage
    {
    }
}