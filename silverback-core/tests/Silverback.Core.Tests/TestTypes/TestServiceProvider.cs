using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Silverback.Tests.TestTypes
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
