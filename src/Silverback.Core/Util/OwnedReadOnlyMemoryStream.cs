// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;

namespace Silverback.Util;

/// <summary>
///     A read-only <see cref="MemoryStream" /> wrapping a buffer that Silverback allocated itself and that nobody else references, such as
///     the output of a serializer. Since the buffer can't be modified through the stream and isn't shared with any user code, it can be
///     handed out without copying it (see <see cref="StreamExtensions.ReadAll" />). The array returned by <c>ReadAll</c> may therefore
///     be the stream's own buffer and must not be modified.
/// </summary>
/// <remarks>
///     Never wrap an array received from the user (e.g. a raw message content), because the user may modify or reuse it.
/// </remarks>
internal sealed class OwnedReadOnlyMemoryStream : MemoryStream
{
    public OwnedReadOnlyMemoryStream(byte[] buffer)
        : base(Check.NotNull(buffer), false)
    {
        Buffer = buffer;
    }

    public byte[] Buffer { get; }
}
