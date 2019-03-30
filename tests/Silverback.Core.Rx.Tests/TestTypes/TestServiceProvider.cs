// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Silverback.Tests.Core.Rx.TestTypes
{
    public class TestServiceProvider
    {
        public static IServiceProvider Create<T>(params T[] instances)
            where T:class
        {
            var services = new ServiceCollection();

            foreach (var instance in instances)
            {
                services.AddSingleton<T>(_ => instance);
            }

            return services.BuildServiceProvider();
        }
    }
}
