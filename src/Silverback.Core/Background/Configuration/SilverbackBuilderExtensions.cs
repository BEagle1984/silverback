// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Background;
using Silverback.Messaging.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class SilverbackBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="IDistributedLockManager"/> implementation and uses the specified DbContext to
        /// handle the distributed locks.
        /// </summary>
        public static ISilverbackBuilder AddDbDistributedLockManager(this ISilverbackBuilder builder)
        {
            builder.Services.AddSingleton<IDistributedLockManager, DbDistributedLockManager>();

            return builder;
        }
    }
}