// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Silverback.Diagnostics;

/// <summary>
///     Wraps the <see cref="ILogger{TCategoryName}" /> mapping the log level according to the <see cref="LogLevelDictionary" /> configuration.
/// </summary>
/// <typeparam name="TCategoryName">
///     The type whose name is used for the logger category name.
/// </typeparam>
[SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Used to init logger category")]
internal interface IMappedLevelsLogger<out TCategoryName> : IMappedLevelsLogger;
