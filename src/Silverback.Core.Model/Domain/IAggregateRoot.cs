// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Domain
{
    /// <summary>
    ///     This empty interface has no other purpose than help recognizing the aggregate root.
    /// </summary>
    [SuppressMessage("", "CA1040", Justification = Justifications.MarkerInterface)]
    public interface IAggregateRoot
    {
    }
}
