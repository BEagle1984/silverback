// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Util;

internal record MergedAction<T>(string Key, Action<T> Action);
