// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Background;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class DependencyInjectionExtensions
    {
        public static IServiceCollection AddBackgroundTaskManager(this IServiceCollection services) =>
            AddBackgroundTaskManager<NullLockManager>(services);

        public static IServiceCollection AddBackgroundTaskManager<TDistributedLockManager>(this IServiceCollection services)
            where TDistributedLockManager : class, IDistributedLockManager
        {
            return services
                .AddSingleton<IBackgroundTaskManager, BackgroundTaskManager>()
                .AddSingleton<IDistributedLockManager, TDistributedLockManager>();
        }
    }
}