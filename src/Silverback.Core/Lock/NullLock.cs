// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Lock;

internal sealed class NullLock : DistributedLock
{
    private NullLock()
    {
    }

    public static NullLock Instance { get; } = new();

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Returned to be disposed by the caller")]
    protected override ValueTask<DistributedLockHandle> AcquireCoreAsync(CancellationToken cancellationToken) =>
        ValueTask.FromResult<DistributedLockHandle>(new NullLockHandle());

    private sealed class NullLockHandle : DistributedLockHandle
    {
        public override CancellationToken LockLostToken => CancellationToken.None;

        protected override void Dispose(bool disposing)
        {
        }

        protected override ValueTask DisposeCoreAsync() => ValueTask.CompletedTask;
    }
}
