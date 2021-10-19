// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
#if !NET5_0_OR_GREATER
    internal static class MethodImplOptionsExtended
    {
        public const MethodImplOptions AggressiveOptimization = (MethodImplOptions)0x0200;
    }
#endif
}
