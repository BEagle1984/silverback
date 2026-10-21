// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     Represents a Kafka partition number. Same shape as the type of the Confluent.Kafka based integration, so that the code written
///     against it compiles unchanged, without exposing the underlying client library.
/// </summary>
public readonly record struct Partition : IComparable<Partition>
{
    /// <summary>
    ///     A special value meaning that any partition can be used (e.g. let the partitioner decide).
    /// </summary>
    public static readonly Partition Any = new(-1);

    /// <summary>
    ///     Initializes a new instance of the <see cref="Partition" /> struct.
    /// </summary>
    /// <param name="value">
    ///     The partition number.
    /// </param>
    public Partition(int value)
    {
        Value = value;
    }

    /// <summary>
    ///     Gets the partition number.
    /// </summary>
    public int Value { get; }

    /// <summary>
    ///     Gets a value indicating whether this is the special <see cref="Any" /> value rather than an actual partition.
    /// </summary>
    public bool IsSpecial => Value == Any.Value;

    /// <summary>
    ///     Converts the specified partition number to a <see cref="Partition" />.
    /// </summary>
    /// <param name="value">
    ///     The partition number.
    /// </param>
    public static implicit operator Partition(int value) => new(value);

    /// <summary>
    ///     Converts the specified <see cref="Partition" /> to its partition number.
    /// </summary>
    /// <param name="partition">
    ///     The partition.
    /// </param>
    public static implicit operator int(Partition partition) => partition.Value;

    /// <summary>
    ///     Compares two partitions.
    /// </summary>
    /// <param name="left">
    ///     The left operand.
    /// </param>
    /// <param name="right">
    ///     The right operand.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the left partition number is lower.
    /// </returns>
    public static bool operator <(Partition left, Partition right) => left.Value < right.Value;

    /// <summary>
    ///     Compares two partitions.
    /// </summary>
    /// <param name="left">
    ///     The left operand.
    /// </param>
    /// <param name="right">
    ///     The right operand.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the left partition number is greater.
    /// </returns>
    public static bool operator >(Partition left, Partition right) => left.Value > right.Value;

    /// <summary>
    ///     Compares two partitions.
    /// </summary>
    /// <param name="left">
    ///     The left operand.
    /// </param>
    /// <param name="right">
    ///     The right operand.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the left partition number is lower or equal.
    /// </returns>
    public static bool operator <=(Partition left, Partition right) => left.Value <= right.Value;

    /// <summary>
    ///     Compares two partitions.
    /// </summary>
    /// <param name="left">
    ///     The left operand.
    /// </param>
    /// <param name="right">
    ///     The right operand.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the left partition number is greater or equal.
    /// </returns>
    public static bool operator >=(Partition left, Partition right) => left.Value >= right.Value;

    /// <summary>
    ///     Creates a <see cref="Partition" /> from the specified partition number.
    /// </summary>
    /// <param name="value">
    ///     The partition number.
    /// </param>
    /// <returns>
    ///     The <see cref="Partition" />.
    /// </returns>
    public static Partition FromInt32(int value) => new(value);

    /// <summary>
    ///     Returns the partition number.
    /// </summary>
    /// <returns>
    ///     The partition number.
    /// </returns>
    public int ToInt32() => Value;

    /// <inheritdoc cref="IComparable{T}.CompareTo" />
    public int CompareTo(Partition other) => Value.CompareTo(other.Value);

    /// <inheritdoc cref="object.ToString" />
    public override string ToString() => IsSpecial ? "[Any]" : $"[{Value}]";
}
