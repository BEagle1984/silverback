// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Background;
using Silverback.Messaging.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>AddDbDistributedLockManager</c> method to the <see cref="ISilverbackBuilder"/>.
    /// </summary>
    public static class SilverbackBuilderExtensions
    {
        /// <summary>
        ///     Adds the <see cref="IDistributedLockManager" /> implementation and uses the specified DbContext to
        ///     handle the distributed locks.
        /// </summary>
        /// <param name="builder">The <see cref="ISilverbackBuilder"/> to add the model types to.</param>
        /// <returns>The <see cref="ISilverbackBuilder"/> so that additional calls can be chained.</returns>
        public static ISilverbackBuilder AddDbDistributedLockManager(this ISilverbackBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.AddSingleton<IDistributedLockManager, DbDistributedLockManager>();

            return builder;
        }
    }
}
