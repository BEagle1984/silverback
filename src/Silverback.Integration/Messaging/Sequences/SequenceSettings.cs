// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Sequences.Batch;

namespace Silverback.Messaging.Sequences
{
    /// <summary>
    ///     The sequence handling settings.
    /// </summary>
    public sealed class SequenceSettings : IEquatable<SequenceSettings>, IValidatableEndpointSettings
    {
        /// <summary>
        ///     Gets or sets the timeout after which an incomplete sequence that isn't pushed with new messages will
        ///     be aborted and discarded. The default is a conservative 30 minutes.
        /// </summary>
        /// <remarks>
        ///     This setting is ignored for batches (<see cref="BatchSequence" />), use the
        ///     <see cref="BatchSettings.MaxWaitTime" /> instead.
        /// </remarks>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(30);

        /// <inheritdoc cref="IValidatableEndpointSettings.Validate" />
        public void Validate()
        {
            if (Timeout <= TimeSpan.Zero)
                throw new EndpointConfigurationException("Sequence.Timeout must be greater than 0.");

            if (Timeout.TotalMilliseconds > int.MaxValue)
            {
                throw new EndpointConfigurationException(
                    "Sequence.Timeout.TotalMilliseconds must be lower or equal to Int32.MaxValue.");
            }
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(SequenceSettings? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return Timeout == other.Timeout;
        }

        /// <inheritdoc cref="object.Equals(object)" />
        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((SequenceSettings)obj);
        }

        /// <inheritdoc cref="object.GetHashCode" />
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode", Justification = Justifications.Settings)]
        public override int GetHashCode() => HashCode.Combine(Timeout);
    }
}
