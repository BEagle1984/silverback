// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

#if !NET5_0_OR_GREATER
// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

internal static class MethodImplOptionsExtended
{
    public const MethodImplOptions AggressiveOptimization = (MethodImplOptions)0x0200;
}

#endif
