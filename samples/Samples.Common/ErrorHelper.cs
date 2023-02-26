using System;

namespace Silverback.Samples.Common;

public static class ErrorHelper
{
    private static readonly Random Random = new((int)DateTime.Now.Ticks);

    public static void RandomlyThrow()
    {
        if (Random.Next(2) == 1)
            throw new InvalidOperationException("You must fail!");
    }
}
