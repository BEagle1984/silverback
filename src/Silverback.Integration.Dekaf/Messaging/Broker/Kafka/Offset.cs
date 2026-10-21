// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     Represents a Kafka offset. Same shape as the type of the Confluent.Kafka based integration, so that the code written
///     against it compiles unchanged, without exposing the underlying client library. The special offsets
///     keep the same negative values, so that the stored offsets remain interchangeable.
/// </summary>
public readonly record struct Offset : IComparable<Offset>
{
    /// <summary>
    ///     A special value referring to the beginning of the partition.
    /// </summary>
    public static readonly Offset Beginning = new(BeginningValue);

    /// <summary>
    ///     A special value referring to the end of the partition.
    /// </summary>
    public static readonly Offset End = new(EndValue);

    /// <summary>
    ///     A special value referring to the stored (committed) offset.
    /// </summary>
    public static readonly Offset Stored = new(StoredValue);

    /// <summary>
    ///     A special value meaning that the offset is not set.
    /// </summary>
    public static readonly Offset Unset = new(UnsetValue);

    private const long BeginningValue = -2;

    private const long EndValue = -1;

    private const long StoredValue = -1000;

    private const long UnsetValue = -1001;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Offset" /> struct.
    /// </summary>
    /// <param name="value">
    ///     The offset value.
    /// </param>
    public Offset(long value)
    {
        Value = value;
    }

    /// <summary>
    ///     Gets the offset value.
    /// </summary>
    public long Value { get; }

    /// <summary>
    ///     Gets a value indicating whether this is one of the special values rather than an actual offset.
    /// </summary>
    public bool IsSpecial => Value == Beginning.Value || Value == End.Value || Value == Stored.Value || Value == Unset.Value;

    /// <summary>
    ///     Converts the specified value to an <see cref="Offset" />.
    /// </summary>
    /// <param name="value">
    ///     The offset value.
    /// </param>
    public static implicit operator Offset(long value) => new(value);

    /// <summary>
    ///     Converts the specified <see cref="Offset" /> to its value.
    /// </summary>
    /// <param name="offset">
    ///     The offset.
    /// </param>
    public static implicit operator long(Offset offset) => offset.Value;

    /// <summary>
    ///     Adds the specified value to the offset.
    /// </summary>
    /// <param name="left">
    ///     The offset.
    /// </param>
    /// <param name="right">
    ///     The value to add.
    /// </param>
    /// <returns>
    ///     The resulting <see cref="Offset" />.
    /// </returns>
    public static Offset operator +(Offset left, long right) => new(left.Value + right);

    /// <summary>
    ///     Compares two offsets.
    /// </summary>
    /// <param name="left">
    ///     The left operand.
    /// </param>
    /// <param name="right">
    ///     The right operand.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the left offset is lower.
    /// </returns>
    public static bool operator <(Offset left, Offset right) => left.Value < right.Value;

    /// <summary>
    ///     Compares two offsets.
    /// </summary>
    /// <param name="left">
    ///     The left operand.
    /// </param>
    /// <param name="right">
    ///     The right operand.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the left offset is greater.
    /// </returns>
    public static bool operator >(Offset left, Offset right) => left.Value > right.Value;

    /// <summary>
    ///     Compares two offsets.
    /// </summary>
    /// <param name="left">
    ///     The left operand.
    /// </param>
    /// <param name="right">
    ///     The right operand.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the left offset is lower or equal.
    /// </returns>
    public static bool operator <=(Offset left, Offset right) => left.Value <= right.Value;

    /// <summary>
    ///     Compares two offsets.
    /// </summary>
    /// <param name="left">
    ///     The left operand.
    /// </param>
    /// <param name="right">
    ///     The right operand.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the left offset is greater or equal.
    /// </returns>
    public static bool operator >=(Offset left, Offset right) => left.Value >= right.Value;

    /// <summary>
    ///     Adds the specified value to the offset.
    /// </summary>
    /// <param name="left">
    ///     The offset.
    /// </param>
    /// <param name="right">
    ///     The value to add.
    /// </param>
    /// <returns>
    ///     The resulting <see cref="Offset" />.
    /// </returns>
    public static Offset Add(Offset left, long right) => left + right;

    /// <summary>
    ///     Creates an <see cref="Offset" /> from the specified value.
    /// </summary>
    /// <param name="value">
    ///     The offset value.
    /// </param>
    /// <returns>
    ///     The <see cref="Offset" />.
    /// </returns>
    public static Offset FromInt64(long value) => new(value);

    /// <summary>
    ///     Returns the offset value.
    /// </summary>
    /// <returns>
    ///     The offset value.
    /// </returns>
    public long ToInt64() => Value;

    /// <inheritdoc cref="IComparable{T}.CompareTo" />
    public int CompareTo(Offset other) => Value.CompareTo(other.Value);

    /// <inheritdoc cref="object.ToString" />
    public override string ToString() => Value switch
    {
        BeginningValue => "Beginning [-2]",
        EndValue => "End [-1]",
        StoredValue => "Stored [-1000]",
        UnsetValue => "Unset [-1001]",
        _ => Value.ToString(CultureInfo.InvariantCulture)
    };
}
