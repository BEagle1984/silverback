// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Silverback.Diagnostics
{
    internal class LogLevelDictionary : Dictionary<EventId, Func<Exception, LogLevel, LogLevel>>, ILogLevelDictionary
    {
    }
}
