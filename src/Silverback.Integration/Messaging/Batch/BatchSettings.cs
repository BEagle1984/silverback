// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Batch
{
    /// <summary>
    ///     The batch consuming settings. To enable batch consuming just set the <c> Size </c> property to a
    ///     value greater than 1.
    /// </summary>
    public class BatchSettings : IEquatable<BatchSettings>, IValidatableEndpointSettings
    {
        /// <summary>
        ///     Gets or sets the number of messages to be processed in batch. Setting this property to a value
        ///     greater than 1 enables batch consuming.
        /// </summary>
        public int Size { get; set; } = 1;

        /// <summary>
        ///     Gets or sets the maximum amount of time to wait for the batch to be filled. After this time the
        ///     batch will be processed even if the specified <c> Size </c> is not reached.
        /// </summary>
        public TimeSpan MaxWaitTime { get; set; } = TimeSpan.MaxValue;

        /// <inheritdoc />
        public bool Equals(BatchSettings other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return Size == other.Size &&
                   MaxWaitTime.Equals(other.MaxWaitTime);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((BatchSettings)obj);
        }

        /// <inheritdoc />
        [SuppressMessage(
            "ReSharper",
            "BaseObjectGetHashCodeCallInGetHashCode",
            Justification = Justifications.MutableProperties)]
        public override int GetHashCode() => base.GetHashCode();

        /// <inheritdoc />
        public void Validate()
        {
            if (Size < 1)
                throw new EndpointConfigurationException("Batch.Size must be greater or equal to 1.");

            if (MaxWaitTime <= TimeSpan.Zero)
                throw new EndpointConfigurationException("Batch.MaxWaitTime must be greater than 0.");
        }
    }
}
