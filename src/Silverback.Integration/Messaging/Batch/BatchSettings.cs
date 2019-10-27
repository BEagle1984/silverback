// Copyright (c) 2019 Sergio Aquilini
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

        #region IEquatable

        public bool Equals(BatchSettings other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Size == other.Size &&
                   MaxWaitTime.Equals(other.MaxWaitTime);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((BatchSettings) obj);
        }

        #endregion

        public void Validate()
        {
            if (Size < 1)
                throw new EndpointConfigurationException("Batch.Size must be greater or equal to 1.");

            if (MaxWaitTime <= TimeSpan.Zero)
                throw new EndpointConfigurationException("Batch.MaxWaitTime must be greater than 0.");

        }
    }
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
}