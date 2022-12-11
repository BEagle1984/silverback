// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Diagnostics;

/// <summary>
///     Used to perform logging in Silverback.
/// </summary>
/// <typeparam name="TCategoryName">
///     The type who's name is used for the logger category name.
/// </typeparam>
[SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Used to init logger category")]
public interface ISilverbackLogger<out TCategoryName> : ISilverbackLogger
{
}
