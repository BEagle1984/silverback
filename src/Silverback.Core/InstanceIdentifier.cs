// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback
{
    /// <summary>
    ///     The identifier used to distinguish the instances of the same type. Used mostly for logging and debugging.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Using this class instead of a <see cref="Guid" /> allocates more memory (about 152 bytes, instead of
    ///         the 16 bytes used by a <see cref="Guid" />) because the value is stored directly as string to avoid
    ///         extra allocations in the <see cref="ToString" /> method (e.g. when writing to a log).
    ///     </para>
    ///     <para>
    ///         This is also a reference type and passing is around requires less allocations.
    ///     </para>
    /// </remarks>
    public sealed class InstanceIdentifier : IEquatable<InstanceIdentifier>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="InstanceIdentifier" /> class.
        /// </summary>
        /// <param name="value">
        ///     The <see cref="Guid" /> representing the identifier value. If <c>null</c> a random one will be generated.
        /// </param>
        public InstanceIdentifier(Guid? value = null)
        {
            Value = (value ?? Guid.NewGuid()).ToString();
        }

        /// <summary>
        ///     Gets the identifier.
        /// </summary>
        public string Value { get; }

        /// <summary>
        ///     Converts the <see cref="InstanceIdentifier" /> to a string.
        /// </summary>
        /// <param name="identifier">
        ///     The <see cref="InstanceIdentifier" /> to be converted.
        /// </param>
        /// <returns>
        ///     The identifier value.
        /// </returns>
        public static implicit operator string(InstanceIdentifier? identifier) =>
            identifier?.ToString() ?? string.Empty;

        /// <inheritdoc cref="op_Equality" />
        public static bool operator ==(InstanceIdentifier? left, InstanceIdentifier? right) =>
            Equals(left, right);

        /// <inheritdoc cref="op_Inequality" />
        public static bool operator !=(InstanceIdentifier? left, InstanceIdentifier? right) =>
            !Equals(left, right);

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(InstanceIdentifier? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Value == other.Value;
        }

        /// <inheritdoc cref="object.Equals(object)" />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((InstanceIdentifier)obj);
        }

        /// <inheritdoc cref="object.GetHashCode" />
        public override int GetHashCode() => HashCode.Combine(Value);

        /// <summary>
        ///     Converts the <see cref="InstanceIdentifier" /> to a string.
        /// </summary>
        /// <returns>
        ///     The identifier value.
        /// </returns>
        public override string ToString() => Value;
    }
}
