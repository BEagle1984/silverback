// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Tests.Integration.TestTypes.Domain
{
    public class BinaryMessage
    {
        public Guid MessageId { get; set; }

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        [SuppressMessage("", "CA1819", Justification = Justifications.CanExposeByteArray)]
        public byte[]? Content { get; set; }
    }
}
