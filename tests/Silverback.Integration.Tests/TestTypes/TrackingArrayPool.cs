// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Buffers;
using System.Collections.Generic;

namespace Silverback.Tests.Integration.TestTypes;

/// <summary>
///     An <see cref="ArrayPool{T}" /> that tracks the rented arrays to detect leaks, double returns and the use of an array after it was
///     returned. The rented arrays are longer than requested and prefilled with garbage, to detect the code relying on the array length
///     or on its initial content. The returned arrays are overwritten with <see cref="ReturnedArrayFill" />.
/// </summary>
public sealed class TrackingArrayPool : ArrayPool<byte>
{
    public const byte RentedArrayFill = 0xAA;

    public const byte ReturnedArrayFill = 0xDD;

    public const int ExtraLength = 7;

    private readonly System.Threading.Lock _lock = new();

    private readonly List<byte[]> _outstandingArrays = [];

    private readonly List<bool> _returnedClearArrayFlags = [];

    private readonly List<int> _requestedLengths = [];

    public int RentedCount
    {
        get
        {
            lock (_lock)
            {
                return _requestedLengths.Count;
            }
        }
    }

    public IReadOnlyList<int> RequestedLengths
    {
        get
        {
            lock (_lock)
            {
                return [.. _requestedLengths];
            }
        }
    }

    public int ReturnedCount
    {
        get
        {
            lock (_lock)
            {
                return _returnedClearArrayFlags.Count;
            }
        }
    }

    public int OutstandingCount
    {
        get
        {
            lock (_lock)
            {
                return _outstandingArrays.Count;
            }
        }
    }

    public IReadOnlyList<bool> ReturnedClearArrayFlags
    {
        get
        {
            lock (_lock)
            {
                return [.. _returnedClearArrayFlags];
            }
        }
    }

    public override byte[] Rent(int minimumLength)
    {
        byte[] array = new byte[minimumLength + ExtraLength];
        Array.Fill(array, RentedArrayFill);

        lock (_lock)
        {
            _requestedLengths.Add(minimumLength);
            _outstandingArrays.Add(array);
        }

        return array;
    }

    public override void Return(byte[] array, bool clearArray = false)
    {
        ArgumentNullException.ThrowIfNull(array);

        lock (_lock)
        {
            int index = _outstandingArrays.FindIndex(outstandingArray => ReferenceEquals(outstandingArray, array));

            if (index < 0)
                throw new InvalidOperationException("The array was already returned or wasn't rented from this pool.");

            _outstandingArrays.RemoveAt(index);
            _returnedClearArrayFlags.Add(clearArray);
        }

        Array.Fill(array, ReturnedArrayFill);
    }
}
