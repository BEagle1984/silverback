// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Silverback.Examples.Common.TestHost
{
    public interface ITestHost : IDisposable
    {
        IServiceProvider ServiceProvider { get; }

        void Run();

        ITestHost ConfigureServices(Action<IServiceCollection> configurationAction);
    }
}
