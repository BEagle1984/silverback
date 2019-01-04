// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Batch
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class BatchSettings : IEquatable<BatchSettings>
    {
        /// <summary>
        /// The number of messages to be processed in batch.
        /// </summary>
        public int Size { get; set; } = 1;

        /// <summary>
        /// The maximum amount of time to wait for the batch to be filled. After this
        /// time the batch will be processed even if the BatchSize is not reached.
        /// </summary>
        public TimeSpan MaxWaitTime { get; set; } = TimeSpan.MaxValue;

        /// <summary>
        /// The maximum number of parallel threads used to process the messages in the batch.
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = 1;

        #region IEquatable

        public bool Equals(BatchSettings other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Size == other.Size &&
                   MaxWaitTime.Equals(other.MaxWaitTime) && 
                   MaxDegreeOfParallelism == other.MaxDegreeOfParallelism;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((BatchSettings) obj);
        }

        #endregion
    }
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
}