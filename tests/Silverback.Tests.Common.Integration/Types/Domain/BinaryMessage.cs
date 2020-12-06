// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Tests.Types.Domain
{
    public class BinaryMessage
    {
        public Guid MessageId { get; set; }

        [SuppressMessage("", "CA1819", Justification = Justifications.CanExposeByteArray)]
        public byte[]? Content { get; set; }
    }
}
