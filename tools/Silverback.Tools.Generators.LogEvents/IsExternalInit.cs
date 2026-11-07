// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

// The compiler looks for the type in this namespace
#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;
#pragma warning restore IDE0130

// Required by the records, because generators must target netstandard2.0
internal static class IsExternalInit;
