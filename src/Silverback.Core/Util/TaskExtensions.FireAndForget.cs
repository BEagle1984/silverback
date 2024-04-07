// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Silverback.Util;

internal static partial class TaskExtensions
{
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Extension method")]
    public static void FireAndForget(this Task task)
    {
        // This method is used just to trick the compiler and avoid CS4014
    }

    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Extension method")]
    public static void FireAndForget(this ValueTask task)
    {
        // This method is used just to trick the compiler and avoid CS4014
    }

    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Extension method")]
    public static void FireAndForget<T>(this Task<T> task)
    {
        // This method is used just to trick the compiler and avoid CS4014
    }

    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Extension method")]
    public static void FireAndForget<T>(this ValueTask<T> task)
    {
        // This method is used just to trick the compiler and avoid CS4014
    }
}
