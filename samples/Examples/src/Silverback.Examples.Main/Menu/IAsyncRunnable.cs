// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Examples.Main.Menu
{
    public interface IAsyncRunnable
    {
        Task Run();
    }
}
