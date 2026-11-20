// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Silverback.Tests.Extended.Benchmarks.Common;
using Silverback.Util;

namespace Silverback.Tests.Extended.Benchmarks.Micro;

/// <summary>
///     Measures the <c>StreamExtensions</c> reading a whole stream (from a <see cref="MemoryStream" />, which is copied, and from any
///     other stream) or the first bytes of it.
/// </summary>
[Config(typeof(MicroBenchmarkConfig))]
[MemoryDiagnoser]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Disposed by the GlobalCleanup method")]
public class StreamExtensionsBenchmark
{
    private const int ReadCount = 1024;

    private MemoryStream? _memoryStream;

    private OtherStream? _otherStream;

    [Params(1024, 64 * 1024)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        byte[] content = new byte[Length];
        new Random(42).NextBytes(content);

        _memoryStream = new MemoryStream(content);
        _otherStream = new OtherStream(new MemoryStream(content));
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _memoryStream?.Dispose();
        _otherStream?.Dispose();
    }

    [Benchmark]
    public byte[]? ReadAllFromMemoryStream() => _memoryStream!.ReadAll();

    [Benchmark]
    public async ValueTask<byte[]?> ReadAllAsyncFromMemoryStream() => await _memoryStream!.ReadAllAsync();

    [Benchmark]
    public byte[]? ReadAllFromOtherStream()
    {
        _otherStream!.Position = 0;
        return _otherStream.ReadAll();
    }

    [Benchmark]
    public async ValueTask<byte[]?> ReadAllAsyncFromOtherStream()
    {
        _otherStream!.Position = 0;
        return await _otherStream.ReadAllAsync();
    }

    [Benchmark]
    public byte[]? Read()
    {
        _memoryStream!.Position = 0;
        return _memoryStream.Read(ReadCount);
    }

    [Benchmark]
    public async ValueTask<byte[]?> ReadAsync()
    {
        _memoryStream!.Position = 0;
        return await _memoryStream.ReadAsync(ReadCount);
    }

    // Not a MemoryStream, so that the extensions copy it to a temporary one
    private sealed class OtherStream : Stream
    {
        private readonly MemoryStream _inner;

        public OtherStream(MemoryStream inner)
        {
            _inner = inner;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override int Read(Span<byte> buffer) => _inner.Read(buffer);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            _inner.ReadAsync(buffer, cancellationToken);

        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _inner.Dispose();

            base.Dispose(disposing);
        }
    }
}
