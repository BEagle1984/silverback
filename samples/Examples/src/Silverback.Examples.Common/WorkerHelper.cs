// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Examples.Common
{
    public static class WorkerHelper
    {
        public static void LoopUntilCancelled()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            Console.CancelKeyPress += OnCancelKeyPress;

            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    Task.Delay(5000, cancellationTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                }
            }

            void OnCancelKeyPress(object sender, ConsoleCancelEventArgs args)
            {
                args.Cancel = true;
                cancellationTokenSource.Cancel();
                Console.CancelKeyPress -= OnCancelKeyPress;
            }
        }
    }
}
