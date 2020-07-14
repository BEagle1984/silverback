// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Silverback.Diagnostics
{
    /// <summary>
    /// Maps <see cref="EventId"/>s to the <see cref="LogLevel"/> that should be used for it.
    /// </summary>
    public interface ILogLevelMapping : IReadOnlyDictionary<EventId, LogLevel>
    {
    }
}
