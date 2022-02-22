// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Lock;

internal sealed record NullLockSettings() : DistributedLockSettings("null");
