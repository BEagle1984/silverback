// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.Logging;

namespace Silverback.Diagnostics
{
    /// <summary>
    ///     Custom logger for Silverback.
    /// </summary>
    /// <typeparam name="TCategoryName">The type who's name is used for the logger category name.</typeparam>
    public interface ISilverbackLogger<out TCategoryName> : ILogger<TCategoryName>, ISilverbackLogger
    {
    }
}
