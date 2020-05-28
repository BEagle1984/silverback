// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     The event args used by the <see cref="InMemoryConsumer.CommitCalled" /> and
    ///     <see cref="InMemoryConsumer.RollbackCalled" /> event.
    /// </summary>
    public class OffsetsEventArgs : EventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="OffsetsEventArgs" /> class.
        /// </summary>
        /// <param name="offsets">
        ///     The offsets that were committed.
        /// </param>
        public OffsetsEventArgs(IReadOnlyCollection<IOffset> offsets)
        {
            Offsets = offsets;
        }

        /// <summary>
        ///     Gets the offsets that were committed or rolled back.
        /// </summary>
        public IReadOnlyCollection<IOffset> Offsets { get; }
    }
}
