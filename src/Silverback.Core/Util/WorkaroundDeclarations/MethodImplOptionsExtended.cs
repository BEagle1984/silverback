// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    /// <summary>
    ///     Adds the <c>MethodImplOptions.AggressiveOptimization</c> option available in .NET 5.
    /// </summary>
    internal static class MethodImplOptionsExtended
    {
        public const MethodImplOptions AggressiveOptimization = (MethodImplOptions)0x0200;
    }
}
