// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.TestBench.Consumer;

public class SimulatedFailureException : Exception
{
    public SimulatedFailureException()
    {
    }

    public SimulatedFailureException(string message)
        : base(message)
    {
    }

    public SimulatedFailureException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
