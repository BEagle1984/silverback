// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback;

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
public sealed record InstanceIdentifier
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

    /// <summary>
    ///     Converts the <see cref="InstanceIdentifier" /> to a string.
    /// </summary>
    /// <returns>
    ///     The identifier value.
    /// </returns>
    public override string ToString() => Value;
}
