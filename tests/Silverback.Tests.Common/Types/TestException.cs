// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Tests.Types;

public class TestException : Exception
{
    public TestException()
    {
    }

    public TestException(string message)
        : base(message)
    {
    }

    public TestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
