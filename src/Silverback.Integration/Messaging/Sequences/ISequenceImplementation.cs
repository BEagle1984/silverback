// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Sequences
{
    /// <summary>
    ///     Represents a set of logically related messages, like the chunks belonging to the same message or the
    ///     messages in a dataset.
    /// </summary>
    internal interface ISequenceImplementation : ISequence
    {
        /// <summary>
        ///     Sets a value indicating whether the first message in the sequence was consumed and this instance was
        ///     just created.
        /// </summary>
        /// <param name="value">
        ///     The value to be set.
        /// </param>
        void SetIsNew(bool value);
    }
}
