// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback
{
    /// <summary>
    ///     Declares a <c>SortIndex</c> property that can be used to properly order the objects implementing this interface.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Used for example to sort the behaviors.
    ///     </para>
    ///     <para>
    ///         The <c>SortBySortIndex</c> extension method can be used to sort the enumerable collections of objects implementing
    ///         this interface.
    ///     </para>
    /// </remarks>
    public interface ISorted
    {
        /// <summary>
        ///     Gets the sort index of this type or .
        /// </summary>
        int SortIndex { get; }
    }
}
