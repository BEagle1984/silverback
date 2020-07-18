// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.Logging;

namespace Silverback.Diagnostics
{
    /// <summary>
    ///     Custom logger for Silverback.
    /// </summary>
    /// <typeparam name="TCategoryName">The category of the logger.</typeparam>
    public interface ISilverbackLogger<out TCategoryName> : ILogger<TCategoryName>, ISilverbackLogger
    {
    }
}
