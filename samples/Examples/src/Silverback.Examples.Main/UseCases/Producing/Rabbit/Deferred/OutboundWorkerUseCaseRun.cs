// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Examples.Common;
using Silverback.Examples.Main.Menu;

namespace Silverback.Examples.Main.UseCases.Producing.Rabbit.Deferred
{
    [SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Invoked by test framework")]
    public class OutboundWorkerUseCaseRun : IAsyncRunnable
    {
        public async Task Run()
        {
            await WorkerHelper.LoopUntilCanceled();

            Console.WriteLine("Canceling...");

            // Let the worker gracefully exit
            await Task.Delay(2000);
        }
    }
}
