// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Util;

internal record MergedAction<T>(string Key, Action<T> Action);
