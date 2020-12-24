// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Util
{
    internal static class CancellationTokenExtensions
    {
        [SuppressMessage("", "VSTHRD200", Justification = "Named after ValueTask.AsTask")]
        public static Task AsTask(this CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => { ((TaskCompletionSource<bool>)s).SetResult(true); }, tcs);
            return tcs.Task;
        }
    }
}
