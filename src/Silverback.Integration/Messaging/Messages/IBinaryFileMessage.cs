// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Represents a binary file that is being transferred over the message broker.
    /// </summary>
    public interface IBinaryFileMessage
    {
        /// <summary>
        ///     Gets or sets the binary content.
        /// </summary>
        [SuppressMessage("ReSharper", "CA1819", Justification = Justifications.CanExposeByteArray)]
        [SuppressMessage("ReSharper", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        byte[]? Content { get; set; }
    }
}