﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.ErrorHandling
{
    public enum ErrorAction
    {
        Skip,
        Retry,
        StopConsuming
    }
}