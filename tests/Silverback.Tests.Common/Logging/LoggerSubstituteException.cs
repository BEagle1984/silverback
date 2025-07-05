// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Tests.Logging;

public class LoggerSubstituteException : Exception
{
    public LoggerSubstituteException()
    {
    }

    public LoggerSubstituteException(string message)
        : base(message)
    {
    }

    public LoggerSubstituteException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
