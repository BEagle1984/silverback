// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Represent a file that is being transmitted over the message broker.
    /// </summary>
    public interface IBinaryFileMessage
    {
        /// <summary>
        ///     Gets or sets the actual file binary content.
        /// </summary>
        [SuppressMessage("ReSharper", "CA1819", Justification = Justifications.CanExposeByteArray)]
        [SuppressMessage("ReSharper", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        byte[]? Content { get; set; }
    }
}