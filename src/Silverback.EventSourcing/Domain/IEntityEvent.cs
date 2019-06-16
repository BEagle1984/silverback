// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Domain
{
    public interface IEntityEvent
    {
        DateTime Timestamp { get; set; }

        int Sequence { get; set; }
    }
}