// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Background;

namespace Silverback.Configuration
{
    /// <content>
    ///     Adds the AddDbDistributedLockManager method to the <see cref="SilverbackBuilder" />.
    /// </content>
    public partial class SilverbackBuilder
    {
        /// <summary>
        ///     Adds the <see cref="IDistributedLockManager" /> implementation that uses the database to handle the distributed locks.
        /// </summary>
        /// <returns>
        ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        // TODO: E2E test this!
        public SilverbackBuilder AddDbDistributedLockManager()
        {
            Services.AddSingleton<IDistributedLockManager, DbDistributedLockManager>();

            return this;
        }
    }
}
