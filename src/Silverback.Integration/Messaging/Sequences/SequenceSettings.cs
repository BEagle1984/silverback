// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Configuration;

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
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(30);

        // TODO: Review summary and find better name (?)!
        /// <summary>
        ///     Gets or sets a value indicating whether the messages belonging to the same sequence are always
        ///     consecutive. The default is <c>true</c>.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Changing this setting to <c>false</c> causes some overhead and it's less efficient in handling the
        ///         incomplete sequences. This may result in the offsets not being properly committed in certain
        ///         circumstances.
        ///     </para>
        ///     <para>
        ///         There can of course be different sequences being processed in different threads, from different
        ///         topics or partitions.
        ///     </para>
        /// </remarks>
        public bool ConsecutiveMessages { get; set; } = true;

        /// <inheritdoc cref="IValidatableEndpointSettings.Validate" />
        public void Validate()
        {
            if (Timeout <= TimeSpan.Zero)
                throw new EndpointConfigurationException("Sequence.Timeout must be greater than 0.");
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
